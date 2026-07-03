using System.Text.Json;
using CloudEventSink.Core.Entities;
using CloudEventSink.Web.Contracts;

namespace CloudEventSink.Web.Mapping;

public static class EventMapper
{
    public static EventListItemResponse ToListItem(EventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new EventListItemResponse
        {
            Id = record.Id,
            SpecVersion = record.SpecVersion,
            EventType = record.EventType,
            EventId = record.EventId,
            EventSource = record.EventSource,
            Subject = record.Subject,
            TimeUtc = record.TimeUtc,
            ReceivedAtUtc = record.ReceivedAtUtc,
        };
    }

    public static EventDetailResponse ToDetail(EventRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new EventDetailResponse
        {
            Id = record.Id,
            SourceId = record.SourceId,
            SpecVersion = record.SpecVersion,
            EventType = record.EventType,
            EventId = record.EventId,
            EventSource = record.EventSource,
            Subject = record.Subject,
            DataContentType = record.DataContentType,
            TimeUtc = record.TimeUtc,
            ReceivedAtUtc = record.ReceivedAtUtc,
            Envelope = ParseJson(record.Envelope),
            Data = ParseJson(record.Data),
        };
    }

    private static JsonElement ParseJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            using JsonDocument empty = JsonDocument.Parse("null");
            return empty.RootElement.Clone();
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
