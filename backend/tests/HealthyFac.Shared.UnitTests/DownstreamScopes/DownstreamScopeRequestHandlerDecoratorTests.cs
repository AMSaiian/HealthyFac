using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.UseCases.Decorators;
using HealthyFac.Shared.UnitTests.DownstreamScopes.Data;

namespace HealthyFac.Shared.UnitTests.DownstreamScopes;

public sealed class DownstreamScopeRequestHandlerDecoratorTests
{
    private readonly DownstreamScopeHolder _holder = new();

    [Fact]
    public async Task DownstreamScopeDecorator_WithDeclaredDownstreamScopes_EntersThemAroundTheHandler()
    {
        var inner = new DownstreamScopeProbeHandler<DeclaringRequest>(_holder);
        var decorator = new DownstreamScopeRequestHandlerDecorator<DeclaringRequest, string>(inner, _holder);

        await decorator.HandleAsync(new DeclaringRequest());

        inner.SeenScopes.Should()
            .Equal(DownstreamScopeTestData.FirstDownstreamScope, DownstreamScopeTestData.SecondDownstreamScope);
    }

    [Fact]
    public async Task DownstreamScopeDecorator_WithoutDeclaration_EntersNothing()
    {
        var inner = new DownstreamScopeProbeHandler<PlainRequest>(_holder);
        var decorator = new DownstreamScopeRequestHandlerDecorator<PlainRequest, string>(inner, _holder);

        await decorator.HandleAsync(new PlainRequest());

        inner.SeenScopes.Should().BeEmpty();
    }

    [Fact]
    public async Task DownstreamScopeDecorator_WithoutDeclarationInsideParentScope_EntersNoDownstreamScopes()
    {
        var inner = new DownstreamScopeProbeHandler<PlainRequest>(_holder);
        var decorator = new DownstreamScopeRequestHandlerDecorator<PlainRequest, string>(inner, _holder);
        using var parent = _holder.Enter(DownstreamScopeTestData.FirstDownstreamScope);

        await decorator.HandleAsync(new PlainRequest());

        inner.SeenScopes.Should().BeEmpty();
    }

    [Fact]
    public void Enter_AfterDispose_RestoresThePreviousDownstreamScopes()
    {
        using var outer = _holder.Enter(DownstreamScopeTestData.FirstDownstreamScope);
        var inner = _holder.Enter(DownstreamScopeTestData.SecondDownstreamScope);

        inner.Dispose();

        _holder.Current.Should().Equal(DownstreamScopeTestData.FirstDownstreamScope);
    }
}
