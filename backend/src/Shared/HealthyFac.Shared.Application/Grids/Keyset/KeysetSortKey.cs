using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// Creates keyset sort keys.
/// </summary>
public static class KeysetSortKey
{
    /// <summary>The key types that a position can carry. One definition for the check and the error message.</summary>
    internal static readonly FrozenSet<Type> SupportedKeyTypes =
        new[] { typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(DateTime), typeof(string) }
            .ToFrozenSet();

    /// <param name="key">The key expression of the sorted row.</param>
    /// <typeparam name="T">The sorted row type.</typeparam>
    /// <typeparam name="TKey">The key type: a non-nullable number, date, or string.</typeparam>
    /// <returns>The sort key.</returns>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TKey"/> is not a supported type.</exception>
    public static IKeysetSortKey<T> For<T, TKey>(Expression<Func<T, TKey>> key)
        where T : IEntity<Guid> => new KeysetSortKey<T, TKey>(key);
}

/// <summary>
/// A keyset sort key over one key expression. The supported key types are <see cref="int"/>, <see cref="long"/>,
/// <see cref="decimal"/>, <see cref="double"/>, <see cref="DateTime"/>, and <see cref="string"/>, never nullable,
/// because a null breaks the comparison of the position. The position keeps the key in JSON with the web defaults.
/// </summary>
/// <typeparam name="T">The sorted row type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public sealed class KeysetSortKey<T, TKey> : IKeysetSortKey<T>
    where T : IEntity<Guid>
{
    private static readonly MethodInfo StringCompare =
        typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo GuidCompareTo = typeof(Guid).GetMethod(nameof(Guid.CompareTo), [typeof(Guid)])!;

    private readonly Expression<Func<T, TKey>> _key;
    private readonly Func<T, TKey> _compiledKey;

    /// <param name="key">The key expression of the sorted row.</param>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TKey"/> is not a supported type.</exception>
    public KeysetSortKey(Expression<Func<T, TKey>> key)
    {
        if (!KeysetSortKey.SupportedKeyTypes.Contains(typeof(TKey)))
        {
            throw GridErrors.KeysetKeyTypeNotSupported(typeof(TKey), KeysetSortKey.SupportedKeyTypes);
        }

        _key = key;
        _compiledKey = key.Compile();
    }

    public IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction) =>
        direction == SortDirection.Descending
            ? query.OrderByDescending(_key).ThenByDescending(row => row.Id)
            : query.OrderBy(_key).ThenBy(row => row.Id);

    public IQueryable<T> After(IQueryable<T> query, SortDirection direction, KeysetPosition after)
    {
        var value = ReadSortValue(after);
        var isDescending = direction == SortDirection.Descending;
        var comparison = isDescending ? ExpressionType.LessThan : ExpressionType.GreaterThan;

        var row = _key.Parameters[index: 0];
        var valueExpression = Expression.Property(
            Expression.Constant(new ValueBox { Value = value }), nameof(ValueBox.Value));
        var idExpression = Expression.Property(
            Expression.Constant(new IdBox { Value = after.Id }), nameof(IdBox.Value));
        var zero = Expression.Constant(value: 0);

        Expression keyAfter;
        Expression keyEqual;
        if (typeof(TKey) == typeof(string))
        {
            var compared = Expression.Call(StringCompare, _key.Body, valueExpression);
            keyAfter = Expression.MakeBinary(comparison, compared, zero);
            keyEqual = Expression.Equal(compared, zero);
        }
        else
        {
            keyAfter = Expression.MakeBinary(comparison, _key.Body, valueExpression);
            keyEqual = Expression.Equal(_key.Body, valueExpression);
        }

        var idAfter = Expression.MakeBinary(
            comparison,
            Expression.Call(Expression.Property(row, nameof(IEntity<Guid>.Id)), GuidCompareTo, idExpression),
            zero);

        var predicate = Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(keyAfter, Expression.AndAlso(keyEqual, idAfter)),
            row);

        return query.Where(predicate);
    }

    public KeysetPosition PositionOf(T row) =>
        new()
        {
            SortValue = JsonSerializer.SerializeToElement(_compiledKey(row), JsonSerializerOptions.Web),
            Id = row.Id,
        };

    private static TKey ReadSortValue(KeysetPosition after)
    {
        try
        {
            var value = after.SortValue.Deserialize<TKey>(JsonSerializerOptions.Web);

            return value switch
            {
                null => throw new JsonException(),
                DateTime { Kind: not DateTimeKind.Utc } => throw new JsonException(),
                _ => value,
            };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new AppValidationException(
            [
                new ValidationFailure(
                    GridErrors.Validation.PositionSortValueKey,
                    GridErrors.Validation.PositionSortValueInvalid),
            ]);
        }
    }

    private sealed class ValueBox
    {
        public required TKey Value { get; init; }
    }

    private sealed class IdBox
    {
        public required Guid Value { get; init; }
    }
}
