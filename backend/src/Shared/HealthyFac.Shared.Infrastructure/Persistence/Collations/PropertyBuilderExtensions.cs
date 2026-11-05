using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Shared.Infrastructure.Persistence.Collations;

/// <summary>
/// Provides extension methods for <see cref="PropertyBuilder{TProperty}"/>.
/// </summary>
public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Opts a text column out of the case-insensitive default (<see cref="ModelBuilderExtensions"/>). Use it only
    /// when two values that differ by case must stay different.
    /// </summary>
    /// <param name="builder">The property builder of the text column.</param>
    /// <returns>The <see cref="PropertyBuilder{TProperty}"/> instance for chained configuration.</returns>
    public static PropertyBuilder<string> IsCaseSensitive(this PropertyBuilder<string> builder) =>
        builder.UseCollation(CollationNames.CaseSensitive);
}
