using HealthyFac.Insights.Application.Agents.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Runs a chat tool in the request scope of the user. The calls to Core, Telemetry, and Document then carry the
/// exchanged token of the user, and each service applies the permissions and the own scope of that user. A chat tool
/// never runs in an advisor scope: the advisor actor would read more than the user may read.
/// </summary>
/// <param name="provider">The service provider of the request.</param>
public sealed class ChatToolScopes(IServiceProvider provider) : IToolScopes
{
    private readonly IServiceProvider _provider = provider;

    /// <summary>
    /// Wraps the request scope. The company comes from the actor of the request, never from the argument.
    /// </summary>
    /// <param name="companyId">Not used: the actor of the request holds the company.</param>
    /// <returns>A scope that does not dispose the request scope.</returns>
    public IServiceScope Create(Guid companyId) => new RequestScope(_provider);

    private sealed class RequestScope(IServiceProvider provider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = provider;

        public void Dispose()
        {
        }
    }
}
