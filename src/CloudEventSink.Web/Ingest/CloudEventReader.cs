using System.Net.Mime;
using System.Text;
using System.Text.Json;
using CloudEventSink.Core.Ingestion;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;

namespace CloudEventSink.Web.Ingest;

public static class CloudEventReader
{
    private static readonly CloudEventFormatter Formatter = new JsonEventFormatter();

    public static bool TryRead(byte[] body, string? contentType, out IncomingEvent incomingEvent)
    {
        ArgumentNullException.ThrowIfNull(body);
        incomingEvent = EmptyEvent();

        if (body.Length == 0)
        {
            return false;
        }

        try
        {
            ContentType parsedContentType = ResolveContentType(contentType);
            using MemoryStream stream = new MemoryStream(body, writable: false);
            CloudEvent cloudEvent = Formatter.DecodeStructuredModeMessage(
                stream,
                parsedContentType,
                extensionAttributes: null
            );

            if (string.IsNullOrEmpty(cloudEvent.Type) || string.IsNullOrEmpty(cloudEvent.Id))
            {
                return false;
            }

            incomingEvent = Build(cloudEvent, body);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ContentType ResolveContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return new ContentType("application/cloudevents+json");
        }

        return new ContentType(contentType);
    }

    private static IncomingEvent Build(CloudEvent cloudEvent, byte[] body)
    {
        string envelopeJson = Encoding.UTF8.GetString(body);
        string dataJson = ExtractData(body);

        return new IncomingEvent
        {
            SpecVersion = cloudEvent.SpecVersion.VersionId,
            EventType = cloudEvent.Type!,
            EventId = cloudEvent.Id!,
            EventSource = cloudEvent.Source?.ToString(),
            Subject = cloudEvent.Subject,
            DataContentType = cloudEvent.DataContentType,
            TimeUtc = cloudEvent.Time,
            EnvelopeJson = envelopeJson,
            DataJson = dataJson,
        };
    }

    private static string ExtractData(byte[] body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        if (
            document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("data", out JsonElement data)
        )
        {
            return data.GetRawText();
        }

        return "null";
    }

    private static IncomingEvent EmptyEvent()
    {
        return new IncomingEvent
        {
            EventType = string.Empty,
            EventId = string.Empty,
            EnvelopeJson = string.Empty,
            DataJson = "null",
        };
    }
}
