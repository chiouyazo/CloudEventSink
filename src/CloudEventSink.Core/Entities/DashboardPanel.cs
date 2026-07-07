using CloudEventSink.Core.Query;

namespace CloudEventSink.Core.Entities;

public sealed class DashboardPanel
{
    public Guid Id { get; set; }

    public Guid DashboardId { get; set; }

    public Guid SavedQueryId { get; set; }

    public required string Title { get; set; }

    public VisualizationKind Visualization { get; set; }

    public int Position { get; set; }
}
