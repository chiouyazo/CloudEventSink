namespace CloudEventSink.Core.Enums;

public enum IngestMode
{
    IgnoreDuplicateById = 0,
    UpsertById = 1,
    UpsertByKey = 2,
    KeepAll = 3,
    KeepOnChange = 4,
}
