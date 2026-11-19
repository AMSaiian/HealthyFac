using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.IntegrationTests.Persistence.Data;

public sealed class SoftDeleteDbContext(DbContextOptions<SoftDeleteDbContext> options) : DbContext(options)
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

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.SaveAsync(this, base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);

    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SoftDeleteTestEntity>().ConfigureEntity().ConfigureSoftDeletable();
        modelBuilder.Entity<PlainTestEntity>().ConfigureEntity();
        modelBuilder.Entity<SoftDeleteGrandchildEntity>().ConfigureEntity().ConfigureSoftDeletable();
        modelBuilder.Entity<SoftDeleteChildEntity>(child =>
        {
            child.ConfigureEntity().ConfigureSoftDeletable();
            child.HasMany(entity => entity.Grandchildren)
                .WithOne()
                .HasForeignKey(grandchild => grandchild.ChildId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlainChildEntity>().ConfigureEntity();
        modelBuilder.Entity<SoftDeleteParentEntity>(parent =>
        {
            parent.ConfigureEntity().ConfigureSoftDeletable();
            parent.HasMany(entity => entity.Children)
                .WithOne()
                .HasForeignKey(child => child.ParentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            parent.HasMany(entity => entity.PlainChildren)
                .WithOne()
                .HasForeignKey(child => child.ParentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SoftDeleteReferenceEntity>(reference =>
        {
            reference.ConfigureEntity().ConfigureSoftDeletable();
            reference.HasOne<SoftDeleteParentEntity>()
                .WithMany()
                .HasForeignKey(entity => entity.ParentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CompositeKeyChildEntity>(child =>
        {
            child.ConfigureSoftDeletable();
            child.HasKey(entity => new { entity.ParentId, entity.Position });
        });
        modelBuilder.Entity<CompositeKeyParentEntity>(parent =>
        {
            parent.ConfigureEntity().ConfigureSoftDeletable();
            parent.HasMany(entity => entity.Children)
                .WithOne()
                .HasForeignKey(child => child.ParentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);
}
