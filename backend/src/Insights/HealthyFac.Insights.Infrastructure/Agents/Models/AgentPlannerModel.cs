using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Planner;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Agents.Models;

/// <summary>
/// Runs the planner as a Microsoft Agent Framework agent over the main chat client. The chat client gets
/// <see cref="FunctionInvokingChatClient"/> with the iteration limit and the error limit of the agent options, so the
/// loop of model and tools ends at a limit and never runs without bound. Each model call goes through the retry of
/// <see cref="RetryingChatClient"/>, which sits below the function loop, so a transient failure repeats one call.
/// The temperature is 0. The trace of the agent holds no prompt and no tool result (<c>EnableSensitiveData</c> is
/// false). When the loop ends and <c>needsDecision</c> says that no write action is buffered, the agent makes one more
/// model call in the same session with only the write tools and a required tool call
/// (<see cref="ChatToolMode.RequireAny"/>). The tool functions are the same, so their validation and buffering apply.
/// </summary>
/// <param name="chatClient">The main chat client.</param>
/// <param name="options">Holds the name of the model and the answer token limit.</param>
/// <param name="agentOptions">Holds the iteration limit and the error limit.</param>
/// <param name="providerCalls">Retries each model call of the loop.</param>
public sealed class AgentPlannerModel(
    IChatClient chatClient,
    IOptions<GeminiOptions> options,
    IOptions<AgentOptions> agentOptions,
    IAgentProviderCalls providerCalls
) : IPlannerModel
{
    public const string AgentName = "planner";

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly AgentOptions _agentOptions = agentOptions.Value;
    private readonly IAgentProviderCalls _providerCalls = providerCalls;

    public string ModelName => _options.ChatModel;

    public async Task<PlannerModelResult> RunAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<AIFunction> tools,
        IReadOnlyList<AIFunction> decisionTools,
        Func<CancellationToken, Task<bool>> needsDecision,
        CancellationToken cancellationToken
    )
    {
        var invoking = new FunctionInvokingChatClient(new RetryingChatClient(_chatClient, _providerCalls))
        {
            MaximumIterationsPerRequest = _agentOptions.PlannerMaxIterations,
            MaximumConsecutiveErrorsPerRequest = _agentOptions.MaxConsecutiveErrors,
        };
        var agent = BuildAgent(invoking, systemPrompt, tools, toolMode: null);

        var session = await agent.CreateSessionAsync(cancellationToken);
        var response = await agent.RunAsync(userMessage, session, cancellationToken: cancellationToken);
        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        var finalText = response.Text;

        if (await needsDecision(cancellationToken))
        {
            var decisionAgent = BuildAgent(invoking, systemPrompt, decisionTools, ChatToolMode.RequireAny);
            var decision = await decisionAgent.RunAsync(
                AgentMessages.PlannerDecisionMessage,
                session,
                cancellationToken: cancellationToken);
            inputTokens += decision.Usage?.InputTokenCount ?? 0;
            outputTokens += decision.Usage?.OutputTokenCount ?? 0;
            finalText = decision.Text;
        }

        return new PlannerModelResult
        {
            InputTokens = (int)inputTokens,
            OutputTokens = (int)outputTokens,
            FinalText = string.IsNullOrWhiteSpace(finalText) ? null : finalText,
        };
    }

    private AIAgent BuildAgent(
        IChatClient invoking,
        string systemPrompt,
        IReadOnlyList<AIFunction> tools,
        ChatToolMode? toolMode
    ) =>
        new ChatClientAgent(
                invoking,
                new ChatClientAgentOptions
                {
                    Name = AgentName,
                    UseProvidedChatClientAsIs = true,
                    ChatOptions = new ChatOptions
                    {
                        Instructions = systemPrompt,
                        Temperature = 0,
                        MaxOutputTokens = _options.MaxOutputTokens,
                        Tools = [.. tools],
                        ToolMode = toolMode,
                    },
                })
            .AsBuilder()
            .UseOpenTelemetry(
                AiServiceExtensions.SourceName,
                telemetry => telemetry.EnableSensitiveData = false)
            .Build();
}
