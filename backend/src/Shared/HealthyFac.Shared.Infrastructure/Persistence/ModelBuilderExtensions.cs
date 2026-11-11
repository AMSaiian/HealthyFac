using HealthyFac.Shared.Infrastructure.Messaging.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.Collations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Shared.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        /// Renames every table and column to snake case, using the same rule as the check constraint builders.
        /// A column name that a configuration set stays (e.g., the row version on the system column <c>xmin</c>).
        /// Call it after the entity configurations, so it sees every entity.
        /// </summary>
        /// <returns>The <see cref="ModelBuilder"/> instance for chained configuration.</returns>
        public ModelBuilder ToSnakeCaseConventions()
        {
            modelBuilder.Model
                .GetEntityTypes()
                .Where(entity =>
                    !string.IsNullOrEmpty(
                        modelBuilder
                            .Entity(entity.Name)
                            .Metadata
                            .GetTableName()))
                .ToList()
                .ForEach(entity =>
                {
                    var currentTableName = modelBuilder
                        .Entity(entity.Name)
                        .Metadata
                        .GetTableName()!;

                    modelBuilder
                        .Entity(entity.Name)
                        .ToTable(currentTableName.ToSnakeCase());

                    entity
                        .GetProperties()
                        .Where(property => property.FindAnnotation(RelationalAnnotationNames.ColumnName) is null)
                        .ToList()
                        .ForEach(property =>
                            modelBuilder.Entity(entity.Name)
                                .Property(property.Name)
                                .HasColumnName(property.Name.ToSnakeCase()));
                });

            return modelBuilder;
        }

        /// <summary>
        /// Creates the <see cref="CollationNames.CaseInsensitive"/> collation and sets it on every text property
        /// that has no collation of its own. A <c>jsonb</c> column is skipped, because PostgreSQL has no collation
        /// for it. Call it after the entity configurations and before <see cref="ToSnakeCaseConventions"/>.
        /// </summary>
        /// <returns>The <see cref="ModelBuilder"/> instance for chained configuration.</returns>
        public ModelBuilder UseCaseInsensitiveText()
        {
            modelBuilder.HasCollation(
                CollationNames.CaseInsensitive,
                CollationNames.CaseInsensitiveLocale,
                CollationNames.IcuProvider,
                deterministic: false);

            modelBuilder.Model
                .GetEntityTypes()
                .SelectMany(entity => entity.GetProperties())
                .Where(property =>
                    property.ClrType == typeof(string)
                    && property.GetCollation() is null
                    && property.GetColumnType() != MessagingPersistenceConstants.PayloadColumnType)
                .ToList()
                .ForEach(property => property.SetCollation(CollationNames.CaseInsensitive));

            return modelBuilder;
        }

        /// <summary>
        /// Registers the PostgreSQL extensions that the calling service requires.
        /// </summary>
        /// <param name="extensions">The PostgreSQL extension names to register on the model.</param>
        /// <returns>The <see cref="ModelBuilder"/> instance for chained configuration.</returns>
        public ModelBuilder HasPostgreSqlExtensions(params string[] extensions)
        {
            foreach (var extension in extensions)
            {
                modelBuilder.HasPostgresExtension(extension);
            }

            return modelBuilder;
        }
    }
}
