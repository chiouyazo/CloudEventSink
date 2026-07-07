using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class EventRecordConfiguration : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("events");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.SpecVersion).HasMaxLength(32);
        builder.Property(record => record.EventType).IsRequired().HasMaxLength(512);
        builder.Property(record => record.EventId).IsRequired().HasMaxLength(512);
        builder.Property(record => record.DedupKey).HasMaxLength(512);
        builder.Property(record => record.GroupKey).HasMaxLength(512);
        builder.Property(record => record.EventSource).HasMaxLength(2048);
        builder.Property(record => record.Subject).HasMaxLength(2048);
        builder.Property(record => record.DataContentType).HasMaxLength(256);
        builder.Property(record => record.TimeUtc);
        builder.Property(record => record.ReceivedAtUtc).IsRequired();
        builder.Property(record => record.Envelope).IsRequired().HasColumnType("jsonb");
        builder.Property(record => record.Data).IsRequired().HasColumnType("jsonb");

        builder.HasIndex(record => new { record.SourceId, record.EventType });
        builder.HasIndex(record => new { record.SourceId, record.ReceivedAtUtc });
        builder.HasIndex(record => new { record.SourceId, record.EventId });
        builder.HasIndex(record => new { record.SourceId, record.DedupKey }).IsUnique();
        builder.HasIndex(record => new
        {
            record.SourceId,
            record.GroupKey,
            record.ReceivedAtUtc,
        });

        builder
            .HasOne<Source>()
            .WithMany()
            .HasForeignKey(record => record.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
