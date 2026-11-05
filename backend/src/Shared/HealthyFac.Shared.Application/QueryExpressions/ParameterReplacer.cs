using System.Linq.Expressions;

namespace HealthyFac.Shared.Application.QueryExpressions;

/// <summary>
/// Puts an expression in place of a lambda parameter. One lambda can then embed another lambda without LINQKit, and
/// EF Core translates the result.
/// </summary>
public sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly Expression _target;

    /// <summary>
    /// Creates a replacer for one parameter.
    /// </summary>
    /// <param name="source">The parameter to replace.</param>
    /// <param name="target">The expression that takes its place.</param>
    public ParameterReplacer(ParameterExpression source, Expression target)
    {
        _source = source;
        _target = target;
    }

    protected override Expression VisitParameter(ParameterExpression node) => node == _source ? _target : node;
}
