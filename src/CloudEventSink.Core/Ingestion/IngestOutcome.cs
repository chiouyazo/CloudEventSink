namespace CloudEventSink.Core.Ingestion;

public enum IngestOutcome
{
    Stored = 0,
    DuplicateIgnored = 1,
    Updated = 2,
}
