using HealthyFac.Insights.Application.Agents;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Infrastructure.Agents.Models;

/// <summary>
/// Sends each model call through the provider retry. It sits between the function loop and the main chat client, so a
/// transient failure repeats only the failed call. After the last retry the call throws
/// <see cref="Application.Ingest.Processing.ProviderUnavailableException"/>.
/// </summary>
/// <param name="innerClient">The main chat client.</param>
/// <param name="providerCalls">Retries a call and maps a stayed failure.</param>
public sealed class RetryingChatClient(IChatClient innerClient, IAgentProviderCalls providerCalls)
    : DelegatingChatClient(innerClient)
{
    private readonly IAgentProviderCalls _providerCalls = providerCalls;

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => _providerCalls.ExecuteAsync(
        token => base.GetResponseAsync(messages, options, token),
        cancellationToken);
}
