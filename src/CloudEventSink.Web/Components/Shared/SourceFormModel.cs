using CloudEventSink.Core.Enums;

namespace CloudEventSink.Web.Components.Shared;

public sealed class SourceFormModel
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public SourceAuthMode AuthMode { get; set; } = SourceAuthMode.Bearer;

    public string? IpAllowlist { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsEdit { get; set; }
}
