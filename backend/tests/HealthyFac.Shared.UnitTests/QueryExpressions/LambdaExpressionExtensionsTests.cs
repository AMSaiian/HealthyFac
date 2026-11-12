using System.Linq.Expressions;
using HealthyFac.Shared.Application.QueryExpressions;
using HealthyFac.Shared.UnitTests.QueryExpressions.Data;

namespace HealthyFac.Shared.UnitTests.QueryExpressions;

public sealed class LambdaExpressionExtensionsTests
{
    [Fact]
    public void Inline_WithArgument_ReplacesOnlyTheLambdaParameter()
    {
        Expression<Func<int, int>> addOffset = value => value + LambdaExpressionExtensionsTestData.Offset;

        var inlined = addOffset.Inline(Expression.Constant(LambdaExpressionExtensionsTestData.Argument));

        var result = Expression.Lambda<Func<int>>(inlined).Compile()();

        result.Should().Be(LambdaExpressionExtensionsTestData.Argument + LambdaExpressionExtensionsTestData.Offset);
    }

    [Fact]
    public void Embed_WithTwoSelectors_ReturnsLambdaOverTheFirstParameterOnly()
    {
        Expression<Func<string, int, bool, bool>> template = (text, length, isLong) => isLong && text.Length > length;
        Expression<Func<string, int>> trimmedLength = value => value.Trim().Length;
        Expression<Func<string, bool>> startsWithPrefix =
            value => value.StartsWith(LambdaExpressionExtensionsTestData.Prefix);

        var embedded = template.Embed(trimmedLength, startsWithPrefix);

        var hasInvocation = new InvocationFinder().Find(embedded);
        var matchesLongText = embedded.Compile()(LambdaExpressionExtensionsTestData.LongText);
        var matchesShortText = embedded.Compile()(LambdaExpressionExtensionsTestData.ShortText);

        embedded.Parameters.Should().ContainSingle();
        hasInvocation.Should().BeFalse();
        matchesLongText.Should().BeTrue();
        matchesShortText.Should().BeFalse();
    }

    private sealed class InvocationFinder : ExpressionVisitor
    {
        private bool _hasInvocation;

        public bool Find(Expression expression)
        {
            Visit(expression);

            return _hasInvocation;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            _hasInvocation = true;

            return base.VisitInvocation(node);
        }
    }
}
