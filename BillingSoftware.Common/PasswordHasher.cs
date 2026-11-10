using System;
using System.Security.Cryptography;
using System.Text;

namespace BillingSoftware.Common;

public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 10000;

    public static string HashPassword(string password, out string salt)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);

        salt = Convert.ToBase64String(algorithm.Salt);
        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        return key;
    }

    public static bool VerifyPassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256);

        var keyToCheck = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        return hash == keyToCheck;
    }
} 
