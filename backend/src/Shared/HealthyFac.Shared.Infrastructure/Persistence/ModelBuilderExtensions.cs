using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Persistence;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        /// Renames every table and column to snake case, using the same rule as the check constraint builders.
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
                        .ToList()
                        .ForEach(property =>
                            modelBuilder.Entity(entity.Name)
                                .Property(property.Name)
                                .HasColumnName(property.Name.ToSnakeCase()));
                });

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
