namespace CloudEventSink.Web.Contracts;

public sealed record QueryColumnDto
{
    public required string Name { get; init; }

    public required string DataType { get; init; }
}
