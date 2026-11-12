using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace HealthyFac.Shared.Application.QueryExpressions;

/// <summary>
/// Composes lambdas without LINQKit. A lambda body replaces a parameter of another lambda, so EF Core translates the
/// result as one expression tree.
/// </summary>
public static class LambdaExpressionExtensions
{
    extension<TSource, TResult>(Expression<Func<TSource, TResult>> lambda)
    {
        /// <summary>
        /// Returns the body of the lambda with <paramref name="argument"/> in place of its only parameter.
        /// </summary>
        /// <param name="argument">The expression that takes the place of the parameter.</param>
        /// <returns>The body of <paramref name="lambda"/> with <paramref name="argument"/> embedded.</returns>
        public Expression Inline(Expression argument) =>
            ReplacingExpressionVisitor.Replace(lambda.Parameters[index: 0], argument, lambda.Body);
    }

    extension<TSource, TPart, TResult>(Expression<Func<TSource, TPart, TResult>> template)
    {
        /// <summary>
        /// Replaces the second parameter of the template with the body of <paramref name="selector"/>. The first
        /// parameter of the template stays the parameter of the result.
        /// </summary>
        /// <param name="selector">The lambda that computes the second parameter from the source.</param>
        /// <returns>A lambda over the first parameter of the template.</returns>
        public Expression<Func<TSource, TResult>> Embed(Expression<Func<TSource, TPart>> selector)
        {
            var sourceParameter = template.Parameters[index: 0];
            var body = ReplacingExpressionVisitor.Replace(
                template.Parameters[index: 1],
                selector.Inline(sourceParameter),
                template.Body
            );

            return Expression.Lambda<Func<TSource, TResult>>(body, sourceParameter);
        }
    }

    extension<TSource, TFirst, TSecond, TResult>(Expression<Func<TSource, TFirst, TSecond, TResult>> template)
    {
        /// <summary>
        /// Replaces the second and the third parameters of the template with the bodies of the two selectors. The
        /// first parameter of the template stays the parameter of the result.
        /// </summary>
        /// <param name="firstSelector">The lambda that computes the second parameter from the source.</param>
        /// <param name="secondSelector">The lambda that computes the third parameter from the source.</param>
        /// <returns>A lambda over the first parameter of the template.</returns>
        public Expression<Func<TSource, TResult>> Embed(
            Expression<Func<TSource, TFirst>> firstSelector,
            Expression<Func<TSource, TSecond>> secondSelector
        )
        {
            var sourceParameter = template.Parameters[index: 0];
            var firstBody = firstSelector.Inline(sourceParameter);
            var secondBody = secondSelector.Inline(sourceParameter);
            var bodyWithFirst = ReplacingExpressionVisitor.Replace(
                template.Parameters[index: 1],
                firstBody,
                template.Body
            );
            var body = ReplacingExpressionVisitor.Replace(template.Parameters[index: 2], secondBody, bodyWithFirst);

            return Expression.Lambda<Func<TSource, TResult>>(body, sourceParameter);
        }
    }
}
