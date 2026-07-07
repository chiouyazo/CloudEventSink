using CloudEventSink.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudEventSink.Infrastructure.Persistence.Configurations;

public sealed class ApiTokenConfiguration : IEntityTypeConfiguration<ApiToken>
{
    public void Configure(EntityTypeBuilder<ApiToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("api_tokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.Name).IsRequired().HasMaxLength(200);
        builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(token => token.TokenLastFour).IsRequired().HasMaxLength(8);
        builder.Property(token => token.CreatedBy).IsRequired().HasMaxLength(256);
        builder.Property(token => token.CreatedAtUtc).IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();
    }
}
