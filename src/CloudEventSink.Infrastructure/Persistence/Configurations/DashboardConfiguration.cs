using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class DashboardConfiguration : IEntityTypeConfiguration<Dashboard>
{
    public void Configure(EntityTypeBuilder<Dashboard> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("dashboards");
        builder.HasKey(dashboard => dashboard.Id);

        builder.Property(dashboard => dashboard.Name).IsRequired().HasMaxLength(200);
        builder.Property(dashboard => dashboard.Description).HasMaxLength(1000);
        builder.Property(dashboard => dashboard.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(dashboard => dashboard.CreatedAtUtc).IsRequired();
        builder.Property(dashboard => dashboard.UpdatedAtUtc).IsRequired();
    }
}
