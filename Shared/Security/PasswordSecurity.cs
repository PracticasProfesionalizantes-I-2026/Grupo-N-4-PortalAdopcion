namespace PortalAdopcion.Shared.Security;
using System.Security.Cryptography;

public static class PasswordSecurity
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA256, 32);
        return $"210000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        try
        {
            var p = encoded.Split('.');
            if (p.Length != 3 || p[0] != "210000")
                return false;
            var got = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(p[1]), 210000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(got, Convert.FromBase64String(p[2]));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
