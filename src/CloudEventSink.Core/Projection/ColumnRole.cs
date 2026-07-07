namespace CloudEventSink.Core.Projection;

public enum ColumnRole
{
    Scalar = 0,
    EventId = 1,
    ReceivedAt = 2,
    EventTime = 3,
    Ordinal = 4,
}
