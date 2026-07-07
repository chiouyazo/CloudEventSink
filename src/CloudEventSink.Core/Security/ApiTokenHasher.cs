using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace CloudEventSink.Core.Security;

public static class ApiTokenHasher
{
    private const int TokenByteLength = 32;

    public static string Generate()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenByteLength));
    }

    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static string LastFour(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        return token.Length >= 4 ? token[^4..] : token;
    }
}
