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
    /// Configures the baseline primary key for entities implementing <see cref="IEntity"/>.
    /// The database generates the identifier, so a new entity carries <see cref="Guid.Empty"/> until it is
    /// saved. Treat <see cref="Guid.Empty"/> as the marker for an unsaved entity.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasDefaultValueSql(PersistenceConstants.IdentifierSql)
            .ValueGeneratedOnAdd();

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
}
