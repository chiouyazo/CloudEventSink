using CloudEventSink.Core.Query;

namespace CloudEventSink.Web.Components.Explore;

public sealed class ExploreFilterRow
{
    public string View { get; set; } = string.Empty;

    public string Column { get; set; } = string.Empty;

    public ConditionOperator Operator { get; set; } = ConditionOperator.Eq;

    public string Value { get; set; } = string.Empty;
}
