using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Shared.UnitTests.DownstreamScopes.Data;

public static class DownstreamScopeTestData
{
    public const string FirstDownstreamScope = "provisioning";
    public const string SecondDownstreamScope = "reporting";
}

[DownstreamScopes(DownstreamScopeTestData.FirstDownstreamScope, DownstreamScopeTestData.SecondDownstreamScope)]
public sealed record DeclaringRequest : IRequest<string>;

public sealed record PlainRequest : IRequest<string>;

/// <summary>
/// Plays the inner handler. It records the downstream scopes that it sees while it runs.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <param name="holder">The holder to read.</param>
public sealed class DownstreamScopeProbeHandler<TRequest>(IDownstreamScopeHolder holder)
    : IRequestHandler<TRequest, string>
    where TRequest : IRequest<string>
{
    private readonly IDownstreamScopeHolder _holder = holder;

    public IReadOnlyCollection<string>? SeenScopes { get; private set; }

    public Task<string> HandleAsync(TRequest request, CancellationToken ct = default(CancellationToken))
    {
        SeenScopes = _holder.Current.ToList();

        return Task.FromResult(string.Empty);
    }
}
