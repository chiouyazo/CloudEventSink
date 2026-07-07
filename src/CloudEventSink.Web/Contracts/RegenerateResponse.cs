namespace CloudEventSink.Web.Contracts;

public sealed record RegenerateResponse
{
    public required int Generated { get; init; }
}
