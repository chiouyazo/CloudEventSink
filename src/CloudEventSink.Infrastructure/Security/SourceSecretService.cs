using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Enums;
using Microsoft.AspNetCore.DataProtection;

namespace CloudEventSink.Infrastructure.Security;

public sealed class SourceSecretService : ISourceSecretService
{
    private const string SignaturePrefix = "sha256=";
    private const int SecretByteLength = 32;

    private readonly IDataProtector protector;

    public SourceSecretService(IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        this.protector = dataProtectionProvider.CreateProtector("CloudEventSink.SourceSecret.v1");
    }

    public IssuedSecret Issue(SourceAuthMode authMode)
    {
        byte[] raw = RandomNumberGenerator.GetBytes(SecretByteLength);
        string plaintext = Base64Url.EncodeToString(raw);
        string lastFour = plaintext.Length >= 4 ? plaintext[^4..] : plaintext;

        string stored = authMode switch
        {
            SourceAuthMode.Bearer => HashToken(plaintext),
            SourceAuthMode.Hmac => this.protector.Protect(plaintext),
            _ => throw new ArgumentOutOfRangeException(
                nameof(authMode),
                authMode,
                "Unsupported authentication mode."
            ),
        };

        return new IssuedSecret
        {
            PlaintextSecret = plaintext,
            StoredValue = stored,
            LastFour = lastFour,
        };
    }

    public bool VerifyBearer(string presentedToken, string storedValue)
    {
        if (string.IsNullOrEmpty(presentedToken) || string.IsNullOrEmpty(storedValue))
        {
            return false;
        }

        byte[] presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presentedToken));

        try
        {
            byte[] storedHash = Convert.FromHexString(storedValue);
            return CryptographicOperations.FixedTimeEquals(presentedHash, storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool VerifyHmac(
        string storedValue,
        ReadOnlySpan<byte> body,
        string presentedSignatureHeader
    )
    {
        if (string.IsNullOrEmpty(storedValue) || string.IsNullOrEmpty(presentedSignatureHeader))
        {
            return false;
        }

        if (!TryParseSignature(presentedSignatureHeader, out byte[] presentedSignature))
        {
            return false;
        }

        try
        {
            string secret = this.protector.Unprotect(storedValue);
            byte[] computed = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
            return CryptographicOperations.FixedTimeEquals(computed, presentedSignature);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string HashToken(string plaintext)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
    }

    private static bool TryParseSignature(string header, out byte[] signature)
    {
        signature = [];
        string trimmed = header.Trim();
        if (!trimmed.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string hex = trimmed[SignaturePrefix.Length..];

        try
        {
            signature = Convert.FromHexString(hex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
