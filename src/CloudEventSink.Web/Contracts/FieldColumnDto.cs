namespace CloudEventSink.Web.Contracts;

public sealed record FieldColumnDto
{
    public required string Name { get; init; }

    public required string SqlType { get; init; }
}
