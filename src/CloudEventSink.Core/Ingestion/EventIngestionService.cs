using System.Text.Json;
using System.Text.Json.Nodes;
using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Enums;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Ingestion;

public sealed class EventIngestionService : IEventIngestionService
{
    private readonly ISourceRepository sourceRepository;
    private readonly IEventRepository eventRepository;
    private readonly ISchemaRepository schemaRepository;
    private readonly ISchemaInferenceService inferenceService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public EventIngestionService(
        ISourceRepository sourceRepository,
        IEventRepository eventRepository,
        ISchemaRepository schemaRepository,
        ISchemaInferenceService inferenceService,
        IUnitOfWork unitOfWork,
        IClock clock
    )
    {
        this.sourceRepository = sourceRepository;
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

        Source? source = await sourceRepository
            .GetByIdAsync(sourceId, cancellationToken)
            .ConfigureAwait(false);
        IngestMode mode = source?.Mode ?? IngestMode.IgnoreDuplicateById;
        DateTimeOffset receivedAt = clock.UtcNow;

        if (mode == IngestMode.KeepOnChange)
        {
            string? groupKey = DedupKeyResolver.Resolve(
                incomingEvent.DataJson,
                source?.DedupKeyPaths
            );
            if (groupKey is not null)
            {
                EventRecord? latest = await eventRepository
                    .GetLatestByGroupKeyAsync(sourceId, groupKey, cancellationToken)
                    .ConfigureAwait(false);
                if (latest is not null && JsonDataEquals(latest.Data, incomingEvent.DataJson))
                {
                    return IngestOutcome.DuplicateIgnored;
                }
            }

            await InsertAsync(
                    sourceId,
                    incomingEvent,
                    receivedAt,
                    null,
                    groupKey,
                    cancellationToken
                )
                .ConfigureAwait(false);
            return IngestOutcome.Stored;
        }

        string? dedupKey = ResolveDedupKey(mode, incomingEvent, source);

        if (
            mode == IngestMode.IgnoreDuplicateById
            && dedupKey is not null
            && await eventRepository
                .GetByDedupKeyAsync(sourceId, dedupKey, cancellationToken)
                .ConfigureAwait(false)
                is not null
        )
        {
            return IngestOutcome.DuplicateIgnored;
        }

        if (mode is IngestMode.UpsertById or IngestMode.UpsertByKey && dedupKey is not null)
        {
            EventRecord? existing = await eventRepository
                .GetByDedupKeyAsync(sourceId, dedupKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                existing.SpecVersion = incomingEvent.SpecVersion;
                existing.EventType = incomingEvent.EventType;
                existing.EventId = incomingEvent.EventId;
                existing.EventSource = incomingEvent.EventSource;
                existing.Subject = incomingEvent.Subject;
                existing.DataContentType = incomingEvent.DataContentType;
                existing.TimeUtc = incomingEvent.TimeUtc;
                existing.ReceivedAtUtc = receivedAt;
                existing.Envelope = incomingEvent.EnvelopeJson;
                existing.Data = incomingEvent.DataJson;

                await UpdateSchemaAsync(sourceId, incomingEvent, receivedAt, cancellationToken)
                    .ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return IngestOutcome.Updated;
            }
        }

        await InsertAsync(sourceId, incomingEvent, receivedAt, dedupKey, null, cancellationToken)
            .ConfigureAwait(false);
        return IngestOutcome.Stored;
    }

    private async Task InsertAsync(
        Guid sourceId,
        IncomingEvent incomingEvent,
        DateTimeOffset receivedAt,
        string? dedupKey,
        string? groupKey,
        CancellationToken cancellationToken
    )
    {
        EventRecord record = new EventRecord
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            SpecVersion = incomingEvent.SpecVersion,
            EventType = incomingEvent.EventType,
            EventId = incomingEvent.EventId,
            DedupKey = dedupKey,
            GroupKey = groupKey,
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
    }

    private static bool JsonDataEquals(string left, string right)
    {
        try
        {
            return JsonNode.DeepEquals(JsonNode.Parse(left), JsonNode.Parse(right));
        }
        catch (JsonException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }

    private static string? ResolveDedupKey(
        IngestMode mode,
        IncomingEvent incomingEvent,
        Source? source
    )
    {
        return mode switch
        {
            IngestMode.KeepAll => null,
            IngestMode.UpsertByKey => DedupKeyResolver.Resolve(
                incomingEvent.DataJson,
                source?.DedupKeyPaths
            ),
            _ => string.IsNullOrEmpty(incomingEvent.EventId) ? null : incomingEvent.EventId,
        };
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
