using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CloudEventSink.Core.Projection;

public static class ProjectionNaming
{
    private const int MaxIdentifierLength = 63;

    public static string ToSnake(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "field";
        }

        StringBuilder builder = new StringBuilder(value.Length + 8);
        char previous = '\0';
        foreach (char current in value)
        {
            if (char.IsAsciiLetterUpper(current))
            {
                if (builder.Length > 0 && previous != '_' && !char.IsAsciiLetterUpper(previous))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else if (char.IsAsciiLetterLower(current) || char.IsAsciiDigit(current))
            {
                builder.Append(current);
            }
            else
            {
                builder.Append('_');
            }

            previous = current;
        }

        string collapsed = CollapseUnderscores(builder.ToString());
        if (collapsed.Length == 0)
        {
            return "field";
        }

        if (char.IsAsciiDigit(collapsed[0]))
        {
            collapsed = "_" + collapsed;
        }

        return collapsed;
    }

    public static string MainViewName(string sourceSlug, string eventType)
    {
        return Cap($"v_{ToSnake(sourceSlug)}_{ToSnake(eventType)}");
    }

    public static string ChildViewName(
        string sourceSlug,
        string eventType,
        IReadOnlyList<string> arrayPath
    )
    {
        ArgumentNullException.ThrowIfNull(arrayPath);
        string suffix = string.Join("_", arrayPath.Select(ToSnake));
        return Cap($"v_{ToSnake(sourceSlug)}_{ToSnake(eventType)}_{suffix}");
    }

    private static string CollapseUnderscores(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        bool lastUnderscore = false;
        foreach (char current in value)
        {
            if (current == '_')
            {
                if (!lastUnderscore && builder.Length > 0)
                {
                    builder.Append('_');
                }

                lastUnderscore = true;
            }
            else
            {
                builder.Append(current);
                lastUnderscore = false;
            }
        }

        return builder.ToString().Trim('_');
    }

    private static string Cap(string name)
    {
        if (name.Length <= MaxIdentifierLength)
        {
            return name;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        string suffix = Convert.ToHexStringLower(hash)[..8];
        return string.Concat(name.AsSpan(0, MaxIdentifierLength - suffix.Length - 1), "_", suffix);
    }
}
