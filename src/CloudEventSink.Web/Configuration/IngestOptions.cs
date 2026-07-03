namespace CloudEventSink.Web.Configuration;

public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    public long MaxBodyBytes { get; set; } = 1_048_576L;
}
