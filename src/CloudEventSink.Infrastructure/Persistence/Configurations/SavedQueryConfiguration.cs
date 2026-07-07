using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class SavedQueryConfiguration : IEntityTypeConfiguration<SavedQuery>
{
    public void Configure(EntityTypeBuilder<SavedQuery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("saved_queries");
        builder.HasKey(query => query.Id);

        builder.Property(query => query.Name).IsRequired().HasMaxLength(200);
        builder.Property(query => query.Description).HasMaxLength(1000);
        builder.Property(query => query.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(query => query.Sql).HasColumnType("text");
        builder.Property(query => query.ModelJson).HasColumnType("jsonb");
        builder.Property(query => query.RenderConfigJson).IsRequired().HasColumnType("jsonb");
        builder.Property(query => query.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(query => query.CreatedAtUtc).IsRequired();
        builder.Property(query => query.UpdatedAtUtc).IsRequired();

        builder.HasIndex(query => query.CreatedBy);
        builder.HasIndex(query => query.FolderId);

        builder
            .HasOne<QueryFolder>()
            .WithMany()
            .HasForeignKey(query => query.FolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
