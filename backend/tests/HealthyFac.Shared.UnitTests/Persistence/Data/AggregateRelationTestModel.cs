using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.UnitTests.Persistence.Data;

public sealed class PlantedRoot
{
    public Guid Id { get; set; }

    public List<PlantedCascadedChild> CascadedChildren { get; set; } = [];

    public List<PlantedRestrictedChild> RestrictedChildren { get; set; } = [];

    public List<PlantedLinkedRow> LinkedRows { get; set; } = [];

    public List<PlantedOwnedItem> OwnedItems { get; set; } = [];

    public PlantedOwnedValue? OwnedValue { get; set; }
}

public sealed class PlantedCascadedChild
{
    public Guid Id { get; set; }

    public Guid RootId { get; set; }
}

public sealed class PlantedRestrictedChild
{
    public Guid Id { get; set; }

    public Guid RootId { get; set; }
}

public sealed class PlantedLinkedRow
{
    public Guid Id { get; set; }

    public Guid? RootId { get; set; }
}

public sealed class PlantedOwnedItem
{
    public string? Text { get; set; }
}

public sealed class PlantedOwnedValue
{
    public string? Text { get; set; }
}

public sealed class PlantedOtherRoot
{
    public Guid Id { get; set; }
}

public sealed class PlantedRestrictedReference
{
    public Guid Id { get; set; }

    public Guid OtherRootId { get; set; }
}

public sealed class PlantedCascadedReference
{
    public Guid Id { get; set; }

    public Guid OtherRootId { get; set; }
}

public sealed class PlantedSetNullReference
{
    public Guid Id { get; set; }

    public Guid? OtherRootId { get; set; }
}

public sealed class AggregateRelationTestModel(DbContextOptions<AggregateRelationTestModel> options)
    : DbContext(options)
{
    public const string UnusedConnectionString = "Host=unused;Database=unused";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlantedRoot>(root =>
        {
            root.HasMany(candidate => candidate.CascadedChildren)
                .WithOne()
                .HasForeignKey(child => child.RootId)
                .OnDelete(DeleteBehavior.Cascade);
            root.HasMany(candidate => candidate.RestrictedChildren)
                .WithOne()
                .HasForeignKey(child => child.RootId)
                .OnDelete(DeleteBehavior.Restrict);
            root.HasMany(candidate => candidate.LinkedRows)
                .WithOne()
                .HasForeignKey(row => row.RootId)
                .IsRequired(required: false)
                .OnDelete(DeleteBehavior.Restrict);
            root.OwnsMany(candidate => candidate.OwnedItems);
            root.OwnsOne(candidate => candidate.OwnedValue);
        });
        modelBuilder.Entity<PlantedRestrictedReference>()
            .HasOne<PlantedOtherRoot>()
            .WithMany()
            .HasForeignKey(reference => reference.OtherRootId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlantedCascadedReference>()
            .HasOne<PlantedOtherRoot>()
            .WithMany()
            .HasForeignKey(reference => reference.OtherRootId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlantedSetNullReference>()
            .HasOne<PlantedOtherRoot>()
            .WithMany()
            .HasForeignKey(reference => reference.OtherRootId)
            .IsRequired(required: false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
