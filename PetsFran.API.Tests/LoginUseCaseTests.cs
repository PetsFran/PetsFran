using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using NUnit.Framework;
using PetsFran.API.Application.UseCases.Auth.Commands;
using PetsFran.API.Application.UseCases.Auth.Implementations;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Domain.Services;
using PetsFran.API.Models.DTOs.PetShops;
using PetsFran.API.Models.Entities;

namespace PetsFran.API.Tests.Application;

/// <summary>
/// Teste de exemplo mostrando como a Clean Architecture facilita testes
/// Apenas interfaces e mocks - nenhuma dependência de banco de dados!
/// </summary>
[TestFixture]
public class LoginUseCaseTests
{
    private LoginUseCase _loginUseCase;
    private Mock<IPetShopRepository> _mockPetShopRepository;
    private Mock<ITokenService> _mockTokenService;
    private Mock<IMapper> _mockMapper;

    [SetUp]
    public void Setup()
    {
        _mockPetShopRepository = new Mock<IPetShopRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockMapper = new Mock<IMapper>();

        _loginUseCase = new LoginUseCase(
            _mockPetShopRepository.Object,
            _mockTokenService.Object,
            _mockMapper.Object);
    }

    [Test]
    public async Task ExecuteAsync_WithNullEmail_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand { Email = null!, Senha = "password" };

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("obrigatório"));
        _mockPetShopRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyEmail_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand { Email = "", Senha = "password" };

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public async Task ExecuteAsync_WithNullPassword_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand { Email = "test@test.com", Senha = null! };

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccess);
    }

    [Test]
    public async Task ExecuteAsync_WithNonExistentEmail_ReturnsFail()
    {
        // Arrange
        var command = new LoginCommand { Email = "notfound@test.com", Senha = "password" };

        _mockPetShopRepository
            .Setup(x => x.GetByEmailAsync("notfound@test.com"))
            .ReturnsAsync((PetShop?)null);

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("Email ou senha"));
    }

    [Test]
    public async Task ExecuteAsync_WithWrongPassword_ReturnsFail()
    {
        // Arrange
        var petShop = new PetShop
        {
            Id = 1,
            Email = "test@test.com",
            Senha = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            RefreshTokenHash = null,
            RefreshTokenExpiryTime = null
        };

        var command = new LoginCommand { Email = "test@test.com", Senha = "wrongPassword" };

        _mockPetShopRepository
            .Setup(x => x.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(petShop);

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.That(result.Message, Does.Contain("Email ou senha"));
    }

    [Test]
    public async Task ExecuteAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var password = "correctPassword";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var petShop = new PetShop
        {
            Id = 1,
            Email = "test@test.com",
            Senha = hashedPassword,
            NomePetShop = "Pet Shop Test",
            RefreshTokenHash = null,
            RefreshTokenExpiryTime = null
        };

        var command = new LoginCommand { Email = "test@test.com", Senha = password };

        _mockPetShopRepository
            .Setup(x => x.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(petShop);

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(It.IsAny<PetShop>()))
            .Returns("access_token_123");

        _mockTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns(("refresh_token_123", System.DateTime.UtcNow.AddDays(7)));

        _mockPetShopRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PetShop>()))
            .ReturnsAsync(petShop);

        _mockMapper
            .Setup(x => x.Map<PetShopBasicDto>(It.IsAny<PetShop>()))
            .Returns(new PetShopBasicDto { Id = 1, Email = "test@test.com", NomePetShop = "Pet Shop Test" });

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.That(result.Data!.Token, Is.EqualTo("access_token_123"));
        Assert.That(result.Data!.RefreshToken, Is.EqualTo("refresh_token_123"));

        // Verify repository was called
        _mockPetShopRepository.Verify(
            x => x.GetByEmailAsync("test@test.com"),
            Times.Once);

        _mockPetShopRepository.Verify(
            x => x.UpdateAsync(It.IsAny<PetShop>()),
            Times.Once);

        _mockTokenService.Verify(
            x => x.GenerateAccessToken(It.IsAny<PetShop>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_PreservesRefreshTokenHashSecurity()
    {
        // Arrange - Garantir que o refresh token é hasheado, não armazenado em plain text
        var password = "correctPassword";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var petShop = new PetShop
        {
            Id = 1,
            Email = "test@test.com",
            Senha = hashedPassword,
            NomePetShop = "Pet Shop Test",
            RefreshTokenHash = null,
            RefreshTokenExpiryTime = null
        };

        var command = new LoginCommand { Email = "test@test.com", Senha = password };
        var plainRefreshToken = "plain_refresh_token_must_be_hashed";

        _mockPetShopRepository
            .Setup(x => x.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(petShop);

        _mockTokenService
            .Setup(x => x.GenerateAccessToken(It.IsAny<PetShop>()))
            .Returns("access_token");

        _mockTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns((plainRefreshToken, System.DateTime.UtcNow.AddDays(7)));

        PetShop? capturedPetShop = null;
        _mockPetShopRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PetShop>()))
            .Callback((PetShop p) => capturedPetShop = p)
            .ReturnsAsync(petShop);

        _mockMapper
            .Setup(x => x.Map<PetShopBasicDto>(It.IsAny<PetShop>()))
            .Returns(new PetShopBasicDto { Id = 1 });

        // Act
        var result = await _loginUseCase.ExecuteAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(capturedPetShop);

        // O token deve ser hasheado, não igual ao plain text
        Assert.That(capturedPetShop!.RefreshTokenHash, Is.Not.EqualTo(plainRefreshToken));

        // Mas o hash deve validar contra o plain token
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify(plainRefreshToken, capturedPetShop.RefreshTokenHash!));
    }
}

