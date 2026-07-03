using CloudEventSink.Core.Abstractions;

namespace CloudEventSink.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
