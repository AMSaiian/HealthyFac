using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HealthyFac.Shared.Domain.Entities;

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
            .IsRequired(false);

        builder.Property(auditable => auditable.UpdatedBy)
            .IsRequired(false);

        return builder;
    }

    /// <summary>
    /// Configures the soft delete columns for entities implementing <see cref="ISoftDeletable"/> and hides deleted
    /// rows from every query with a global query filter. A query that must see deleted rows calls
    /// <c>IgnoreQueryFilters()</c>.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureSoftDeletable<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Property(deletable => deletable.DeletedAt)
            .IsRequired(false);

        builder.Property(deletable => deletable.DeletedBy)
            .IsRequired(false);

        builder.HasQueryFilter(deletable => deletable.DeletedAt == null);

        return builder;
    }
}
