using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using CloudEventSink.Core.Enums;
using CloudEventSink.Core.Ingestion;
using CloudEventSink.Core.Schema;

namespace CloudEventSink.Core.Tests;

public sealed class EventIngestionServiceTests
{
    private static readonly Guid SourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task IgnoreDuplicateById_SecondSameId_IsIgnored()
    {
        FakeEventRepository events = new FakeEventRepository();
        EventIngestionService service = Build(IngestMode.IgnoreDuplicateById, null, events);

        IngestOutcome first = await service.IngestAsync(
            SourceId,
            Event("E1", """{ "v": 1 }"""),
            CancellationToken.None
        );
        IngestOutcome second = await service.IngestAsync(
            SourceId,
            Event("E1", """{ "v": 2 }"""),
            CancellationToken.None
        );

        Assert.Equal(IngestOutcome.Stored, first);
        Assert.Equal(IngestOutcome.DuplicateIgnored, second);
        Assert.Single(events.Store);
    }

    [Fact]
    public async Task UpsertById_SecondSameId_ReplacesData()
    {
        FakeEventRepository events = new FakeEventRepository();
        EventIngestionService service = Build(IngestMode.UpsertById, null, events);

        await service.IngestAsync(SourceId, Event("E1", """{ "v": 1 }"""), CancellationToken.None);
        IngestOutcome second = await service.IngestAsync(
            SourceId,
            Event("E1", """{ "v": 2 }"""),
            CancellationToken.None
        );

        Assert.Equal(IngestOutcome.Updated, second);
        Assert.Single(events.Store);
        Assert.Contains("\"v\": 2", events.Store[0].Data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpsertByKey_SameKey_ReplacesDifferentKey_Inserts()
    {
        FakeEventRepository events = new FakeEventRepository();
        EventIngestionService service = Build(IngestMode.UpsertByKey, "machineId", events);

        await service.IngestAsync(
            SourceId,
            Event("E1", """{ "machineId": "M1", "v": 1 }"""),
            CancellationToken.None
        );
        IngestOutcome sameKey = await service.IngestAsync(
            SourceId,
            Event("E2", """{ "machineId": "M1", "v": 2 }"""),
            CancellationToken.None
        );
        IngestOutcome otherKey = await service.IngestAsync(
            SourceId,
            Event("E3", """{ "machineId": "M2", "v": 3 }"""),
            CancellationToken.None
        );

        Assert.Equal(IngestOutcome.Updated, sameKey);
        Assert.Equal(IngestOutcome.Stored, otherKey);
        Assert.Equal(2, events.Store.Count);
    }

    [Fact]
    public async Task KeepAll_KeepsEveryDelivery()
    {
        FakeEventRepository events = new FakeEventRepository();
        EventIngestionService service = Build(IngestMode.KeepAll, null, events);

        await service.IngestAsync(SourceId, Event("E1", """{ "v": 1 }"""), CancellationToken.None);
        await service.IngestAsync(SourceId, Event("E1", """{ "v": 1 }"""), CancellationToken.None);

        Assert.Equal(2, events.Store.Count);
    }

    [Fact]
    public async Task KeepOnChange_InsertsOnlyWhenDataChanges()
    {
        FakeEventRepository events = new FakeEventRepository();
        EventIngestionService service = Build(IngestMode.KeepOnChange, "machineId", events);

        await service.IngestAsync(
            SourceId,
            Event("E1", """{ "machineId": "M1", "v": 1 }"""),
            CancellationToken.None
        );
        IngestOutcome same = await service.IngestAsync(
            SourceId,
            Event("E2", """{ "machineId": "M1", "v": 1 }"""),
            CancellationToken.None
        );
        IngestOutcome changed = await service.IngestAsync(
            SourceId,
            Event("E3", """{ "machineId": "M1", "v": 2 }"""),
            CancellationToken.None
        );

        Assert.Equal(IngestOutcome.DuplicateIgnored, same);
        Assert.Equal(IngestOutcome.Stored, changed);
        Assert.Equal(2, events.Store.Count);
    }

    [Fact]
    public void DedupKeyResolver_CompositeAndMissing()
    {
        Assert.NotNull(DedupKeyResolver.Resolve("""{ "a": "1", "b": "2" }""", "a,b"));
        Assert.Null(DedupKeyResolver.Resolve("""{ "a": "1" }""", "a,b"));
        Assert.Null(DedupKeyResolver.Resolve("""{ "a": "1" }""", null));
    }

    private static EventIngestionService Build(
        IngestMode mode,
        string? keyPaths,
        FakeEventRepository events
    )
    {
        Source source = new Source
        {
            Id = SourceId,
            Name = "s",
            Slug = "s",
            SecretHash = "h",
            SecretLastFour = "1234",
            Mode = mode,
            DedupKeyPaths = keyPaths,
        };
        return new EventIngestionService(
            new FakeSourceRepository(source),
            events,
            new FakeSchemaRepository(),
            new SchemaInferenceService(),
            new FakeUnitOfWork(),
            new FakeClock()
        );
    }

    private static IncomingEvent Event(string id, string dataJson)
    {
        return new IncomingEvent
        {
            EventType = "order.created",
            EventId = id,
            EnvelopeJson = "{}",
            DataJson = dataJson,
        };
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public List<EventRecord> Store { get; } = new List<EventRecord>();

        public Task<EventRecord?> GetByDedupKeyAsync(
            Guid sourceId,
            string dedupKey,
            CancellationToken cancellationToken
        )
        {
            EventRecord? found = Store.FirstOrDefault(record =>
                record.SourceId == sourceId
                && string.Equals(record.DedupKey, dedupKey, StringComparison.Ordinal)
            );
            return Task.FromResult(found);
        }

        public Task<EventRecord?> GetLatestByGroupKeyAsync(
            Guid sourceId,
            string groupKey,
            CancellationToken cancellationToken
        )
        {
            EventRecord? found = Store
                .Where(record =>
                    record.SourceId == sourceId
                    && string.Equals(record.GroupKey, groupKey, StringComparison.Ordinal)
                )
                .OrderByDescending(record => record.ReceivedAtUtc)
                .LastOrDefault();
            return Task.FromResult(found);
        }

        public void Add(EventRecord eventRecord)
        {
            Store.Add(eventRecord);
        }

        public Task<bool> ExistsAsync(
            Guid sourceId,
            string eventId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(
                Store.Any(record => record.SourceId == sourceId && record.EventId == eventId)
            );
        }

        public Task<EventRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedResult<EventRecord>> ListBySourceAsync(
            Guid sourceId,
            string? eventType,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<long> CountBySourceAsync(Guid sourceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<long> CountReceivedBeforeAsync(
            Guid sourceId,
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<int> DeleteReceivedBeforeAsync(
            Guid sourceId,
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<DateTimeOffset?> LatestReceivedAtAsync(
            Guid sourceId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }

    private sealed class FakeSourceRepository : ISourceRepository
    {
        private readonly Source source;

        public FakeSourceRepository(Source source)
        {
            this.source = source;
        }

        public Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Source?>(source);

        public Task<IReadOnlyList<Source>> ListAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Source?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> SlugExistsAsync(
            string slug,
            Guid? excludingId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<bool> NameExistsAsync(
            string name,
            Guid? excludingId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public void Add(Source source) => throw new NotSupportedException();

        public void Remove(Source source) => throw new NotSupportedException();
    }

    private sealed class FakeSchemaRepository : ISchemaRepository
    {
        private readonly Dictionary<string, InferredSchema> store = new Dictionary<
            string,
            InferredSchema
        >(StringComparer.Ordinal);

        public Task<InferredSchema?> GetAsync(
            Guid sourceId,
            string eventType,
            CancellationToken cancellationToken
        )
        {
            store.TryGetValue(eventType, out InferredSchema? schema);
            return Task.FromResult(schema);
        }

        public Task<IReadOnlyList<InferredSchema>> ListBySourceAsync(
            Guid sourceId,
            CancellationToken cancellationToken
        ) => Task.FromResult<IReadOnlyList<InferredSchema>>(store.Values.ToList());

        public void Add(InferredSchema schema)
        {
            store[schema.EventType] = schema;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
    }
}
