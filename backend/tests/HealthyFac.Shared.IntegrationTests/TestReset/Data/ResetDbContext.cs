using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.TestReset.Data;

public sealed class ResetDbContext(DbContextOptions<ResetDbContext> options) : DbContext(options)
{
    public DbSet<ReferenceItem> ReferenceItems => Set<ReferenceItem>();

    public DbSet<BaseItem> BaseItems => Set<BaseItem>();

    public DbSet<RecordedItem> RecordedItems => Set<RecordedItem>();
}
