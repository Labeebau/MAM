using System.Security.Cryptography;

public static class PasswordHelper
{
    public static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(storedSalt), 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        return Convert.ToBase64String(hash) == storedHash;
    }
}
