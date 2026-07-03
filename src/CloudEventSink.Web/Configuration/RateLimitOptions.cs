namespace CloudEventSink.Web.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int IngestPerMinute { get; set; } = 100;
}
