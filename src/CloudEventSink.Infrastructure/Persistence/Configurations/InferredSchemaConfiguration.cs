using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class InferredSchemaConfiguration : IEntityTypeConfiguration<InferredSchema>
{
    public void Configure(EntityTypeBuilder<InferredSchema> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("schemas");
        builder.HasKey(schema => schema.Id);

        builder.Property(schema => schema.EventType).IsRequired().HasMaxLength(512);
        builder.Property(schema => schema.RootNodeJson).IsRequired().HasColumnType("jsonb");
        builder.Property(schema => schema.SampleCount).IsRequired();
        builder.Property(schema => schema.FirstSeenUtc).IsRequired();
        builder.Property(schema => schema.LastUpdatedUtc).IsRequired();

        builder.HasIndex(schema => new { schema.SourceId, schema.EventType }).IsUnique();

        builder
            .HasOne<Source>()
            .WithMany()
            .HasForeignKey(schema => schema.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
