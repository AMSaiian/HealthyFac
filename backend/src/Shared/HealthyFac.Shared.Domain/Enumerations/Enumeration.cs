using System.Collections.Frozen;
using System.Reflection;

namespace HealthyFac.Shared.Domain.Enumerations;

/// <summary>
/// Base type for a closed set of named domain values. Derive from it, then expose each member as a
/// <see langword="public static readonly"/> field.
/// </summary>
/// <remarks>
/// Each member carries a meaningful camelCase value, for example <c>inMaintenance</c>. Lookups ignore
/// case, matching how native enums deserialize. Two members may therefore not differ by case alone.
/// That value travels
/// over the API and into the database, so treat it as a contract. Never rename one in place.
/// </remarks>
/// <typeparam name="TEnum">The deriving enumeration type.</typeparam>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<MemberCatalog> Catalog =
        new(Discover, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Initializes a new enumeration member.
    /// </summary>
    /// <param name="value">The meaningful camelCase identity of the member.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> holds no text.</exception>
    protected Enumeration(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw EnumerationErrors.ValueRequired(nameof(value));
        }

        Value = value;
    }

    /// <summary>The meaningful camelCase identity of this member.</summary>
    public string Value { get; }

    /// <summary>Every declared member, in declaration order.</summary>
    public static IReadOnlyList<TEnum> All => Catalog.Value.Ordered;

    /// <summary>
    /// Resolves the member carrying the given value.
    /// </summary>
    /// <param name="value">The value to resolve.</param>
    /// <returns>The matching member.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no member carries <paramref name="value"/>.
    /// </exception>
    public static TEnum FromValue(string value)
        => TryFromValue(value, out var member)
            ? member
            : throw EnumerationErrors.UnknownValue(typeof(TEnum), value);

    /// <summary>
    /// Attempts to resolve the member carrying the given value.
    /// </summary>
    /// <param name="value">The value to resolve.</param>
    /// <param name="member">The matching member when the lookup succeeds.</param>
    /// <returns><see langword="true"/> when a member carries <paramref name="value"/>.</returns>
    public static bool TryFromValue(string? value, out TEnum member)
    {
        if (!string.IsNullOrWhiteSpace(value) && Catalog.Value.ByValue.TryGetValue(value, out var found))
        {
            member = found;

            return true;
        }

        member = null!;

        return false;
    }

    /// <inheritdoc/>
    public bool Equals(Enumeration<TEnum>? other)
        => other is not null
            && GetType() == other.GetType()
            && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Enumeration<TEnum>);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public int CompareTo(Enumeration<TEnum>? other)
        => other is null ? 1 : string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override string ToString() => Value;

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => Equals(left, right);

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => !Equals(left, right);

    public static bool operator <(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => left is null || left.CompareTo(right) <= 0;

    public static bool operator >(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => left is null ? right is null : left.CompareTo(right) >= 0;

    private static MemberCatalog Discover()
    {
        var ordered = typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsInitOnly && typeof(TEnum).IsAssignableFrom(field.FieldType))
            .Select(field => (TEnum)field.GetValue(null)!)
            .ToList();

        return new MemberCatalog
        {
            Ordered = ordered,
            ByValue = ordered.ToFrozenDictionary(member => member.Value, StringComparer.OrdinalIgnoreCase),
        };
    }

    private sealed class MemberCatalog
    {
        public required IReadOnlyList<TEnum> Ordered { get; init; }
        public required FrozenDictionary<string, TEnum> ByValue { get; init; }
    }
}
