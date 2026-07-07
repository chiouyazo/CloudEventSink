namespace CloudEventSink.Web.Contracts;

public sealed record FieldViewDto
{
    public required string Name { get; init; }

    public required bool IsChild { get; init; }

    public required string EventType { get; init; }

    public required IReadOnlyList<FieldColumnDto> Columns { get; init; }
}
