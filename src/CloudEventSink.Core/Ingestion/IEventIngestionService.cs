namespace CloudEventSink.Core.Ingestion;

public interface IEventIngestionService
{
    Task<IngestOutcome> IngestAsync(
        Guid sourceId,
        IncomingEvent incomingEvent,
        CancellationToken cancellationToken
    );
}
