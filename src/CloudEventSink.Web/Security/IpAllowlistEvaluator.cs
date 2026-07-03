using System.Net;

namespace CloudEventSink.Web.Security;

public static class IpAllowlistEvaluator
{
    public static bool IsAllowed(string? allowlist, IPAddress? remoteAddress)
    {
        if (string.IsNullOrWhiteSpace(allowlist))
        {
            return true;
        }

        if (remoteAddress is null)
        {
            return false;
        }

        string[] entries = allowlist.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return entries.Any(entry => Matches(entry, remoteAddress));
    }

    private static bool Matches(string entry, IPAddress remoteAddress)
    {
        if (entry.Contains('/', StringComparison.Ordinal))
        {
            return IPNetwork.TryParse(entry, out IPNetwork network)
                && network.Contains(remoteAddress);
        }

        return IPAddress.TryParse(entry, out IPAddress? single) && single.Equals(remoteAddress);
    }
}
