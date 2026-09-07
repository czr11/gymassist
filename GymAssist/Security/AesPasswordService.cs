using System.Security.Cryptography;
using System.Text;

namespace GymAssist.Security;

public sealed class AesPasswordService(IConfiguration configuration)
{
    private readonly byte[] key = LoadKey(configuration);

    public bool Verify(string password, string encryptedPassword)
    {
        try
        {
            var payload = Convert.FromBase64String(encryptedPassword);
            if (payload.Length < 12 + 16)
            {
                return false;
            }

            var nonce = payload.AsSpan(0, 12);
            var tag = payload.AsSpan(12, 16);
            var ciphertext = payload.AsSpan(28);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(password),
                plaintext);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public string Encrypt(string password)
    {
        var plaintext = Encoding.UTF8.GetBytes(password);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    private static byte[] LoadKey(IConfiguration configuration)
    {
        var encodedKey = Environment.GetEnvironmentVariable("AES_KEY")
            ?? configuration["Security:AesKey"];

        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            throw new InvalidOperationException("Configura AES_KEY con una clave Base64 de 32 bytes.");
        }

        try
        {
            var key = Convert.FromBase64String(encodedKey);
            if (key.Length != 32)
            {
                throw new InvalidOperationException("AES_KEY debe representar exactamente 32 bytes.");
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("AES_KEY debe ser una cadena Base64 válida.", exception);
        }
    }
}
