namespace CloudEventSink.Web.Contracts;

public sealed record ErrorResponse
{
    public required string Error { get; init; }
}
