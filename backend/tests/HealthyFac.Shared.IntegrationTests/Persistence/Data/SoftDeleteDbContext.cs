using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteDbContext(DbContextOptions<SoftDeleteDbContext> options) : DbContext(options)
{
    public DbSet<SoftDeleteTestEntity> SoftDeletables => Set<SoftDeleteTestEntity>();

    public DbSet<PlainTestEntity> Plains => Set<PlainTestEntity>();

    public DbSet<SoftDeleteParentEntity> Parents => Set<SoftDeleteParentEntity>();

    public DbSet<SoftDeleteChildEntity> Children => Set<SoftDeleteChildEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SoftDeleteTestEntity>().ConfigureEntity().ConfigureSoftDeletable();
        modelBuilder.Entity<PlainTestEntity>().ConfigureEntity();
        modelBuilder.Entity<SoftDeleteChildEntity>().ConfigureEntity().ConfigureSoftDeletable();
        modelBuilder.Entity<SoftDeleteParentEntity>(parent =>
        {
            parent.ConfigureEntity().ConfigureSoftDeletable();
            parent.HasMany(entity => entity.Children)
                .WithOne()
                .HasForeignKey(child => child.ParentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
