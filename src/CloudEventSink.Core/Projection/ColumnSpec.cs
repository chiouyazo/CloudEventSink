namespace CloudEventSink.Core.Projection;

public sealed record ColumnSpec
{
    public required string Name { get; init; }

    public IReadOnlyList<string> SourcePath { get; init; } = [];

    public string SqlType { get; init; } = "text";

    public bool Included { get; init; } = true;

    public ColumnRole Role { get; init; } = ColumnRole.Scalar;
}
