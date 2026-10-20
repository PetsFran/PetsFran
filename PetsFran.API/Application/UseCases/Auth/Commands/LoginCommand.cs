namespace PetsFran.API.Application.UseCases.Auth.Commands;

public class LoginCommand
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
    public object PetShop { get; set; } = null!;
}

