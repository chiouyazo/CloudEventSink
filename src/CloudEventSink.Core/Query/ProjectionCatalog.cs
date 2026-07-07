namespace CloudEventSink.Core.Query;

public sealed record ProjectionCatalog
{
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Views { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

    public bool HasView(string view)
    {
        return Views.ContainsKey(view);
    }

    public bool TryGetColumnType(string view, string column, out string sqlType)
    {
        sqlType = "text";
        if (!Views.TryGetValue(view, out IReadOnlyDictionary<string, string>? columns))
        {
            return false;
        }

        return columns.TryGetValue(column, out sqlType!);
    }
}
