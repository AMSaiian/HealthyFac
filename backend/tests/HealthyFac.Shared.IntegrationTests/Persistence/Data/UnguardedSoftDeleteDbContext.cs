using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class UnguardedSoftDeleteDbContext(DbContextOptions<UnguardedSoftDeleteDbContext> options)
    : DbContext(options)
{
    public DbSet<SoftDeleteTestEntity> SoftDeletables => Set<SoftDeleteTestEntity>();

    public DbSet<PlainTestEntity> Plains => Set<PlainTestEntity>();

    public DbSet<SoftDeleteParentEntity> Parents => Set<SoftDeleteParentEntity>();

    public DbSet<SoftDeleteChildEntity> Children => Set<SoftDeleteChildEntity>();

    public DbSet<SoftDeleteGrandchildEntity> Grandchildren => Set<SoftDeleteGrandchildEntity>();

    public DbSet<SoftDeleteReferenceEntity> References => Set<SoftDeleteReferenceEntity>();

    public DbSet<PlainChildEntity> PlainChildren => Set<PlainChildEntity>();

    public DbSet<CompositeKeyParentEntity> CompositeParents => Set<CompositeKeyParentEntity>();

    public DbSet<CompositeKeyChildEntity> CompositeChildren => Set<CompositeKeyChildEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        SoftDeleteDbContext.ConfigureModel(modelBuilder);
}
