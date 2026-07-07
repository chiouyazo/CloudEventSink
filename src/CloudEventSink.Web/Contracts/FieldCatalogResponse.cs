namespace CloudEventSink.Web.Contracts;

public sealed record FieldCatalogResponse
{
    public required Guid SourceId { get; init; }

    public required IReadOnlyList<FieldViewDto> Views { get; init; }
}
