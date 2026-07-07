namespace CloudEventSink.Core.Query;

public static class SqlStatementValidator
{
    public static bool IsReadOnlySelect(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        string trimmed = sql.Trim();

        int firstSemicolon = trimmed.IndexOf(';', StringComparison.Ordinal);
        if (firstSemicolon >= 0 && firstSemicolon != trimmed.Length - 1)
        {
            return false;
        }

        string normalized = trimmed.TrimStart('(');
        return normalized.StartsWith("select", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("with", StringComparison.OrdinalIgnoreCase);
    }

    public static void EnsureReadOnlySelect(string sql)
    {
        if (!IsReadOnlySelect(sql))
        {
            throw new QueryValidationException(
                "Only a single read-only SELECT or WITH statement is allowed."
            );
        }
    }
}
