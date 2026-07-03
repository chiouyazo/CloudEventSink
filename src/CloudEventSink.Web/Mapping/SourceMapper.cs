using CloudEventSink.Core.Entities;
using CloudEventSink.Web.Contracts;

namespace CloudEventSink.Web.Mapping;

public static class SourceMapper
{
    public static SourceResponse ToResponse(Source source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new SourceResponse
        {
            Id = source.Id,
            Name = source.Name,
            Slug = source.Slug,
            AuthMode = source.AuthMode,
            SecretLastFour = source.SecretLastFour,
            IpAllowlist = source.IpAllowlist,
            IsActive = source.IsActive,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc,
        };
    }
}
