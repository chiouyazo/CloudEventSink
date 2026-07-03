using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sources");
        builder.HasKey(source => source.Id);

        builder.Property(source => source.Name).IsRequired().HasMaxLength(200);
        builder.Property(source => source.Slug).IsRequired().HasMaxLength(120);
        builder
            .Property(source => source.AuthMode)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(source => source.SecretHash).IsRequired().HasMaxLength(4096);
        builder.Property(source => source.SecretLastFour).IsRequired().HasMaxLength(8);
        builder.Property(source => source.IpAllowlist).HasMaxLength(2048);
        builder.Property(source => source.IsActive).IsRequired();
        builder.Property(source => source.CreatedAtUtc).IsRequired();
        builder.Property(source => source.UpdatedAtUtc).IsRequired();

        builder.HasIndex(source => source.Slug).IsUnique();
        builder.HasIndex(source => source.Name).IsUnique();
    }
}
