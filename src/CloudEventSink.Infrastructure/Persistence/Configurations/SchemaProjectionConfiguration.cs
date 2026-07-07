using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class SchemaProjectionConfiguration : IEntityTypeConfiguration<SchemaProjection>
{
    public void Configure(EntityTypeBuilder<SchemaProjection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("schema_projections");
        builder.HasKey(projection => projection.Id);

        builder.Property(projection => projection.EventType).IsRequired().HasMaxLength(512);
        builder.Property(projection => projection.MainViewName).IsRequired().HasMaxLength(128);
        builder.Property(projection => projection.ColumnsJson).IsRequired().HasColumnType("jsonb");
        builder
            .Property(projection => projection.ChildViewsJson)
            .IsRequired()
            .HasColumnType("jsonb");
        builder.Property(projection => projection.SpecJson).HasColumnType("jsonb");
        builder.Property(projection => projection.GeneratedAtUtc).IsRequired();

        builder
            .HasIndex(projection => new { projection.SourceId, projection.EventType })
            .IsUnique();

        builder
            .HasOne<Source>()
            .WithMany()
            .HasForeignKey(projection => projection.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
