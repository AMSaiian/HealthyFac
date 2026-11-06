using System.Linq.Expressions;
using HealthyFac.Shared.Application.QueryExpressions;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Chains the predicates of the filter fields. A <see langword="null"/> predicate is a field without a value: it adds
/// no condition, so the chain returns the other side.
/// </summary>
public static class Predicates
{
    /// <summary>
    /// Starts a chain. Call <c>And</c> and <c>Or</c> on the result, never directly after <c>?.</c>:
    /// <c>Name?.ToPredicate(...).And(...)</c> skips the whole chain when <c>Name</c> is <see langword="null"/>.
    /// </summary>
    /// <param name="first">The first predicate, or <see langword="null"/> for a field without a value.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns><paramref name="first"/>, as the start of the chain.</returns>
    public static Expression<Func<T, bool>>? From<T>(Expression<Func<T, bool>>? first) => first;

    extension<T>(Expression<Func<T, bool>>? left)
    {
        /// <summary>
        /// Joins two predicates with <c>AndAlso</c>.
        /// </summary>
        /// <param name="right">The second predicate, or <see langword="null"/> for a field without a value.</param>
        /// <returns>
        /// The joined predicate, or <see langword="null"/> when both sides are <see langword="null"/>.
        /// </returns>
        public Expression<Func<T, bool>>? And(Expression<Func<T, bool>>? right) =>
            Join(left, right, Expression.AndAlso);

        /// <summary>
        /// Joins two predicates with <c>OrElse</c>.
        /// </summary>
        /// <param name="right">The second predicate, or <see langword="null"/> for a field without a value.</param>
        /// <returns>
        /// The joined predicate, or <see langword="null"/> when both sides are <see langword="null"/>.
        /// </returns>
        public Expression<Func<T, bool>>? Or(Expression<Func<T, bool>>? right) =>
            Join(left, right, Expression.OrElse);
    }

    /// <summary>
    /// Joins two lambdas into one. Each side comes from its own selector lambda, so the method rebinds the parameter of
    /// <paramref name="right"/> to the parameter of <paramref name="left"/>.
    /// </summary>
    /// <param name="left">The first predicate.</param>
    /// <param name="right">The second predicate.</param>
    /// <param name="join">Builds the binary node (<c>AndAlso</c> or <c>OrElse</c>).</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The joined predicate, or the side that is not <see langword="null"/>.</returns>
    private static Expression<Func<T, bool>>? Join<T>(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right,
        Func<Expression, Expression, BinaryExpression> join
    )
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        var rightBody = new ParameterReplacer(right.Parameters[index: 0], left.Parameters[index: 0]).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(join(left.Body, rightBody), left.Parameters);
    }
}
