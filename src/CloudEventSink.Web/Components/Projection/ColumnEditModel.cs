using CloudEventSink.Core.Projection;

namespace CloudEventSink.Web.Components.Projection;

public sealed class ColumnEditModel
{
    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<string> SourcePath { get; set; } = [];

    public string SqlType { get; set; } = "text";

    public bool Included { get; set; } = true;

    public ColumnRole Role { get; set; } = ColumnRole.Scalar;
}
