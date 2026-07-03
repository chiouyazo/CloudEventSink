using CloudEventSink.Core.Enums;

namespace CloudEventSink.Core.Abstractions;

public interface ISourceSecretService
{
    IssuedSecret Issue(SourceAuthMode authMode);

    bool VerifyBearer(string presentedToken, string storedValue);

    bool VerifyHmac(string storedValue, ReadOnlySpan<byte> body, string presentedSignatureHeader);
}
