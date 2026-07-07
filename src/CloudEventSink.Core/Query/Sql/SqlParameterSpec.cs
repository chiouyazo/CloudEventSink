namespace CloudEventSink.Core.Query.Sql;

public sealed record SqlParameterSpec
{
    public required string Name { get; init; }

    public required SqlParameterType Type { get; init; }

    public object? Value { get; init; }
}
