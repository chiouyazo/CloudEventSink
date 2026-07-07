using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class QueryFolderConfiguration : IEntityTypeConfiguration<QueryFolder>
{
    public void Configure(EntityTypeBuilder<QueryFolder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("query_folders");
        builder.HasKey(folder => folder.Id);

        builder.Property(folder => folder.Name).IsRequired().HasMaxLength(200);
        builder.Property(folder => folder.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(folder => folder.CreatedAtUtc).IsRequired();

        builder
            .HasOne<QueryFolder>()
            .WithMany()
            .HasForeignKey(folder => folder.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
