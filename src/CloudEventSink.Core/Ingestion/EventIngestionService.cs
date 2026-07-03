using System.Text.Json;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Ingestion;

public sealed class EventIngestionService : IEventIngestionService
{
    private readonly IEventRepository eventRepository;
    private readonly ISchemaRepository schemaRepository;
    private readonly ISchemaInferenceService inferenceService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public EventIngestionService(
        IEventRepository eventRepository,
        ISchemaRepository schemaRepository,
        ISchemaInferenceService inferenceService,
        IUnitOfWork unitOfWork,
        IClock clock
    )
    {
        this.eventRepository = eventRepository;
        this.schemaRepository = schemaRepository;
        this.inferenceService = inferenceService;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<IngestOutcome> IngestAsync(
        Guid sourceId,
        IncomingEvent incomingEvent,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(incomingEvent);

        if (
            !string.IsNullOrEmpty(incomingEvent.EventId)
            && await eventRepository
                .ExistsAsync(sourceId, incomingEvent.EventId, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            return IngestOutcome.DuplicateIgnored;
        }

        DateTimeOffset receivedAt = clock.UtcNow;

        EventRecord record = new EventRecord
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            SpecVersion = incomingEvent.SpecVersion,
            EventType = incomingEvent.EventType,
            EventId = incomingEvent.EventId,
            EventSource = incomingEvent.EventSource,
            Subject = incomingEvent.Subject,
            DataContentType = incomingEvent.DataContentType,
            TimeUtc = incomingEvent.TimeUtc,
            ReceivedAtUtc = receivedAt,
            Envelope = incomingEvent.EnvelopeJson,
            Data = incomingEvent.DataJson,
        };
        eventRepository.Add(record);

        await UpdateSchemaAsync(sourceId, incomingEvent, receivedAt, cancellationToken)
            .ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return IngestOutcome.Stored;
    }

    private async Task UpdateSchemaAsync(
        Guid sourceId,
        IncomingEvent incomingEvent,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        FieldNode derived = DeriveFromData(incomingEvent.DataJson);

        InferredSchema? existing = await schemaRepository
            .GetAsync(sourceId, incomingEvent.EventType, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            FieldNode root = inferenceService.RecomputePresence(derived);
            InferredSchema created = new InferredSchema
            {
                Id = Guid.NewGuid(),
                SourceId = sourceId,
                EventType = incomingEvent.EventType,
                RootNodeJson = FieldNodeSerializer.Serialize(root),
                SampleCount = 1L,
                FirstSeenUtc = now,
                LastUpdatedUtc = now,
            };
            schemaRepository.Add(created);
            return;
        }

        FieldNode stored = FieldNodeSerializer.Deserialize(existing.RootNodeJson);
        FieldNode merged = inferenceService.Merge(stored, derived);
        FieldNode recomputed = inferenceService.RecomputePresence(merged);

        existing.RootNodeJson = FieldNodeSerializer.Serialize(recomputed);
        existing.SampleCount += 1L;
        existing.LastUpdatedUtc = now;
    }

    private FieldNode DeriveFromData(string dataJson)
    {
        string payload = string.IsNullOrWhiteSpace(dataJson) ? "null" : dataJson;
        using JsonDocument document = JsonDocument.Parse(payload);
        return inferenceService.Derive(document.RootElement);
    }
}
