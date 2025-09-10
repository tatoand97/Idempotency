using System.Security.Cryptography;
using System.Text;

namespace PruebaIdempotencia.Domain.Idempotency;

public static class Hashing
{
    public static string Sha256Base64(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

