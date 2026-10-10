using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthyFac.Shared.Infrastructure.Persistence;

/// <summary>
/// Stores an <see cref="Enumeration{TEnum}"/> member as its value, and reads it back through the declared
/// members. An unknown value in the column throws, so a renamed member fails loudly instead of silently
/// producing a null.
/// </summary>
/// <typeparam name="TEnum">The enumeration type to convert.</typeparam>
public sealed class EnumerationValueConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : Enumeration<TEnum>
{
    public EnumerationValueConverter()
        : base(
            member => member.Value,
            value => Enumeration<TEnum>.FromValue(value))
    {
    }
}
