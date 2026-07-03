using CloudEventSink.Core.Abstractions;
using CloudEventSink.Core.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CloudEventSink.Infrastructure.Persistence;

public sealed class AppDbContext
    : IdentityDbContext<IdentityUser>,
        IUnitOfWork,
        IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Source> Sources => Set<Source>();

    public DbSet<EventRecord> Events => Set<EventRecord>();

    public DbSet<InferredSchema> Schemas => Set<InferredSchema>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
