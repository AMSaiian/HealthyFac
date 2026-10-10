using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Provides extension methods for mapping enumerations to text columns.
/// </summary>
public static class EnumerationConventionExtensions
{
    extension(ModelConfigurationBuilder configurationBuilder)
    {
        /// <summary>
        /// Registers an enumeration as a scalar type for the whole model, with the text conversion and the
        /// length of its longest value. Call it from <c>ConfigureConventions</c> for every enumeration.
        /// A configuration on a single property is too late: EF Core discovers an enumeration property as a
        /// navigation to an entity type before the entity configurations run, and it fails while it builds the
        /// model, because the enumeration has no public constructor.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to register.</typeparam>
        /// <returns>The <see cref="ModelConfigurationBuilder"/> instance for chained configuration.</returns>
        public ModelConfigurationBuilder HaveEnumerationConversion<TEnum>()
            where TEnum : Enumeration<TEnum>
        {
            configurationBuilder.Properties<TEnum>()
                .HaveConversion<EnumerationValueConverter<TEnum>>()
                .HaveMaxLength(Enumeration<TEnum>.All.Max(member => member.Value.Length));

            return configurationBuilder;
        }
    }
}
