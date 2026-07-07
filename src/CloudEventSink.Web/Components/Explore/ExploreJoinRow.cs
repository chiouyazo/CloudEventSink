using System.Collections.ObjectModel;
using CloudEventSink.Core.Query;

namespace CloudEventSink.Web.Components.Explore;

public sealed class ExploreJoinRow
{
    public string TargetView { get; set; } = string.Empty;

    public JoinType Type { get; set; } = JoinType.Left;

    public string LeftView { get; set; } = string.Empty;

    public Collection<ExploreJoinKey> Keys { get; } = new Collection<ExploreJoinKey>();
}
