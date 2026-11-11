using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Provides extension methods for EntityTypeBuilder to configure reusable entity patterns.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures the baseline primary key for entities implementing <see cref="IEntity"/>. The application
    /// generates the identifier (<see cref="Guid.CreateVersion7()"/>), so the column has no database default and
    /// EF Core always sends the value.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        return builder;
    }

    /// <summary>
    /// Configures baseline auditing properties for entities implementing <see cref="IAuditable"/>.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureAuditable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(auditable => auditable.CreatedAt)
            .IsRequired();

        builder.Property(auditable => auditable.CreatedBy)
            .IsRequired();

        builder.Property(auditable => auditable.UpdatedAt)
            .IsRequired(required: false);

        builder.Property(auditable => auditable.UpdatedBy)
            .IsRequired(required: false);

        return builder;
    }

    /// <summary>
    /// Configures the soft delete columns for entities implementing <see cref="ISoftDeletable"/> and hides deleted
    /// rows from every query with a named global query filter. A query that must see deleted rows calls
    /// <c>IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])</c>.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureSoftDeletable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Property(deletable => deletable.DeletedAt)
            .IsRequired(required: false);

        builder.Property(deletable => deletable.DeletedBy)
            .IsRequired(required: false);

        builder.HasQueryFilter(SoftDeleteConstants.QueryFilterName, deletable => deletable.DeletedAt == null);

        return builder;
    }

    /// <summary>
    /// Maps <see cref="IVersioned.RowVersion"/> to the PostgreSQL system column <c>xmin</c> as a concurrency token,
    /// so a save of an old version throws <c>DbUpdateConcurrencyException</c>. To check the version that a client
    /// sent, set the original value of the property (<c>Entry(entity).Property(...).OriginalValue</c>) before the
    /// save: EF Core compares the original value, and the database writes the current one.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureVersioned<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IVersioned
    {
        builder.Property(versioned => versioned.RowVersion)
            .HasColumnName(PersistenceConstants.RowVersionColumn)
            .IsRowVersion();

        return builder;
    }
}
