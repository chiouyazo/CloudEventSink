namespace CloudEventSink.Web.Components.Explore;

public sealed class SaveQueryInput
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? FolderId { get; set; }
}
