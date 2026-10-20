namespace PetsFran.API.Services.Auth;

public static class PasswordHashVerifier
{
    public static bool Verify(string? password, string? hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
