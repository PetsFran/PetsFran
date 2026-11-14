using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetsFran.API.Controllers;
using PetsFran.API.Data;
using PetsFran.API.Models.DTOs.Precos;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services;
using Xunit;

namespace PetsFran.Tests;

public sealed class PrecosIntegrationTests : IClassFixture<PrecosDatabase>
{
    private readonly PrecosDatabase _database;

    public PrecosIntegrationTests(PrecosDatabase database) => _database = database;

    [Fact]
    public async Task PrecoPersonalizadoAfetaApenasOPetShopQueOAlterou()
    {
        await using var db = _database.CreateContext();
        var primeiro = await CriarPetShop(db);
        var segundo = await CriarPetShop(db);
        var controller = CriarController(db, primeiro.Id);

        var resposta = await controller.AlterarPreco(new AlterarPrecoServicoRequest(
            "Base", "Pequeno", "Vira lata", "Banho", 90m));

        Assert.IsType<OkObjectResult>(resposta.Result);
        var calculadora = new CalculaPrecosService(db);
        Assert.Equal(100m, calculadora.GetPrecoTotalHorario(CriarHorario(primeiro.Id)));
        Assert.Equal(70m, calculadora.GetPrecoTotalHorario(CriarHorario(segundo.Id)));
        Assert.Contains(calculadora.GetCatalogo(primeiro.Id), item =>
            item.Tipo == "Base" && item.Nome == "Banho" && item.Raca == "Vira lata" && item.Personalizado);
        Assert.DoesNotContain(calculadora.GetCatalogo(segundo.Id), item => item.Personalizado);
    }

    [Fact]
    public async Task RestaurarRemoveAAlteracaoERecuperaOPrecoPadrao()
    {
        await using var db = _database.CreateContext();
        var petShop = await CriarPetShop(db);
        var controller = CriarController(db, petShop.Id);
        var identificacao = new IdentificarPrecoServicoRequest("Base", "Pequeno", "Vira lata", "Banho");

        await controller.AlterarPreco(new AlterarPrecoServicoRequest(
            identificacao.Tipo, identificacao.Porte, identificacao.Raca, identificacao.Nome, 90m));
        var resposta = await controller.RestaurarPreco(identificacao);

        Assert.IsType<OkObjectResult>(resposta.Result);
        Assert.False(await db.PrecosServicos.AnyAsync(p => p.PetShopId == petShop.Id));
        Assert.Equal(70m, new CalculaPrecosService(db).GetPrecoTotalHorario(CriarHorario(petShop.Id)));
    }

    [Fact]
    public async Task UmPetShopNaoPodeLerOuExcluirOutroPetShop()
    {
        await using var db = _database.CreateContext();
        var primeiro = await CriarPetShop(db);
        var segundo = await CriarPetShop(db);
        var contextoHttp = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("PetShopId", primeiro.Id.ToString()) }, "test"))
        };
        var controller = new PetShopsController(db, null!, new HttpContextAccessor { HttpContext = contextoHttp })
        {
            ControllerContext = new ControllerContext { HttpContext = contextoHttp }
        };

        var dashboard = await controller.GetDashboard(segundo.Id);
        var exclusao = await controller.Delete(segundo.Id);

        Assert.IsType<ForbidResult>(dashboard.Result);
        Assert.IsType<ForbidResult>(exclusao);
        Assert.True(await db.PetShops.AnyAsync(p => p.Id == segundo.Id));
        Assert.IsType<OkObjectResult>((await controller.GetDashboard(primeiro.Id)).Result);
    }

    private static async Task<PetShop> CriarPetShop(AppDbContext db)
    {
        var petShop = new PetShop { Email = $"teste-{Guid.NewGuid():N}@example.com", Senha = "teste" };
        db.PetShops.Add(petShop);
        await db.SaveChangesAsync();
        return petShop;
    }

    private static ServicosController CriarController(AppDbContext db, int petShopId) => new(db, new CalculaPrecosService(db))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("PetShopId", petShopId.ToString()) }, "test"))
            }
        }
    };

    private static Horario CriarHorario(int petShopId) => new()
    {
        PetShopId = petShopId,
        Cachorro = new Cachorro { Porte = "Pequeno", Raca = "Vira lata" },
        ServicoBaseSelecionado = "Banho",
        Adicionais = new List<string> { "Cortar Unha" }
    };
}

public sealed class PrecosDatabase : IDisposable
{
    private readonly string _adminConnection;
    private readonly string _databaseName = $"petsfran_test_{Guid.NewGuid():N}";
    private readonly string _testConnection;

    public PrecosDatabase()
    {
        _adminConnection = Environment.GetEnvironmentVariable("PETSFRAN_TEST_ADMIN_CONNECTION")
            ?? throw new InvalidOperationException("Defina PETSFRAN_TEST_ADMIN_CONNECTION para um PostgreSQL de testes.");
        var connection = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _databaseName };
        _testConnection = connection.ConnectionString;
        using (var admin = new NpgsqlConnection(_adminConnection))
        {
            admin.Open();
            using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            command.ExecuteNonQuery();
        }
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_testConnection).Options);

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
        using var admin = new NpgsqlConnection(_adminConnection);
        admin.Open();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        command.ExecuteNonQuery();
    }
}
