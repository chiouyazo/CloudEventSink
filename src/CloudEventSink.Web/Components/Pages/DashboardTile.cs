using CloudEventSink.Core.Entities;

namespace CloudEventSink.Web.Components.Pages;

public sealed record DashboardTile
{
    public required Source Source { get; init; }

    public required long EventCount { get; init; }

    public DateTimeOffset? LastActivityUtc { get; init; }
}
