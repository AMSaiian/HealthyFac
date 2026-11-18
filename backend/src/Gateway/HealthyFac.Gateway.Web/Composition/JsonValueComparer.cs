using System.Globalization;
using System.Text.Json;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Compares two sort values that a service sent as JSON. The value must have the JSON kind of
/// <typeparamref name="TKey"/>. Text compares in the invariant culture and ignores case, like the
/// <c>case_insensitive</c> collation of the databases.
/// </summary>
/// <typeparam name="TKey">The type of the sort value (a number, a date, or text).</typeparam>
public sealed class JsonValueComparer<TKey> : IComparer<JsonElement>
    where TKey : notnull, IComparable<TKey>
{
    /// <summary>
    /// Compares two JSON values.
    /// </summary>
    /// <param name="x">The first sort value.</param>
    /// <param name="y">The second sort value.</param>
    /// <returns>The sign of the comparison, as <see cref="IComparer{T}"/> defines it.</returns>
    /// <exception cref="CompositionException">Thrown when a value is not a JSON value of the kind of
    /// <typeparamref name="TKey"/>.</exception>
    public int Compare(JsonElement x, JsonElement y)
    {
        var left = Read(x);
        var right = Read(y);

        return left is string leftText && right is string rightText
            ? string.Compare(leftText, rightText, CultureInfo.InvariantCulture, CompareOptions.IgnoreCase)
            : left.CompareTo(right);
    }

    private static TKey Read(JsonElement value)
    {
        try
        {
            return value.Deserialize<TKey>() ?? throw new CompositionException(CompositionErrors.SortValueMismatch);
        }
        catch (JsonException exception)
        {
            throw new CompositionException(CompositionErrors.SortValueMismatch, exception);
        }
    }
}
