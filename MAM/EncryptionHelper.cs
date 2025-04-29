using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class EncryptionHelper
{
    private static readonly string Passphrase = "SuperSecretPassphrase"; // Must stay same for encryption/decryption
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("FixedSalt1234567"); // Should be 16 bytes ideally

    private static void GenerateKeyAndIV(out byte[] key, out byte[] iv)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(Passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
        key = pbkdf2.GetBytes(32); // 32 bytes = 256 bit key
        iv = pbkdf2.GetBytes(16);  // 16 bytes = 128 bit IV
    }

    public static string Encrypt(string plainText)
    {
        GenerateKeyAndIV(out byte[] key, out byte[] iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string encryptedText)
    {
        GenerateKeyAndIV(out byte[] key, out byte[] iv);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var buffer = Convert.FromBase64String(encryptedText);

        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
