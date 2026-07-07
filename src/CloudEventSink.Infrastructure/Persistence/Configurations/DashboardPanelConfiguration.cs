using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class DashboardPanelConfiguration : IEntityTypeConfiguration<DashboardPanel>
{
    public void Configure(EntityTypeBuilder<DashboardPanel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("dashboard_panels");
        builder.HasKey(panel => panel.Id);

        builder.Property(panel => panel.Title).IsRequired().HasMaxLength(200);
        builder
            .Property(panel => panel.Visualization)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(panel => panel.DashboardId);

        builder
            .HasOne<Dashboard>()
            .WithMany()
            .HasForeignKey(panel => panel.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<SavedQuery>()
            .WithMany()
            .HasForeignKey(panel => panel.SavedQueryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
