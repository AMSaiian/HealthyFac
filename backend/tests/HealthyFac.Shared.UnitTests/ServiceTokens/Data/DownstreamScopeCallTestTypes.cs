using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

[DownstreamScopes(ServiceTokenTestData.FirstDownstreamScope)]
public sealed record DownstreamScopeCallRequest : IRequest<string>;

/// <summary>
/// Plays a use case that calls another service through the client of the harness.
/// </summary>
/// <param name="harness">Gives the client that makes the call.</param>
public sealed class DownstreamScopeCallHandler(ServiceTokenHarness harness)
    : IRequestHandler<DownstreamScopeCallRequest, string>
{
    private readonly ServiceTokenHarness _harness = harness;

    public async Task<string> HandleAsync(
        DownstreamScopeCallRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        using var response = await _harness.CallAsync();

        return string.Empty;
    }
}
