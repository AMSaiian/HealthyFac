using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Extraction.Runs;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Agents.Models;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Extraction;

/// <summary>
/// Runs the extraction as a Microsoft Agent Framework agent over the main chat client with the four extractor tools
/// (<c>propose_machine</c>, <c>propose_metric</c>, <c>propose_status</c>, <c>propose_operating_phase</c>). The
/// function loop stops at <see cref="AgentOptions.ExtractorMaxIterations"/> model calls. A tool that rejects a call
/// sends the errors to the model, which corrects that call. After <see cref="AgentOptions.MaxConsecutiveErrors"/>
/// failed calls in a row the run ends and the extraction fails. Each model call goes through the provider retry, so a
/// transient failure repeats one call. The temperature is 0, and the trace holds no prompt and no tool result
/// (<c>EnableSensitiveData</c> is false).
/// </summary>
/// <param name="chatClient">The main chat client.</param>
/// <param name="options">Holds the name of the model and the answer token limit.</param>
/// <param name="agentOptions">Holds the iteration limit and the error limit.</param>
/// <param name="providerCalls">Retries each model call of the loop.</param>
/// <param name="registry">Builds the functions of the extractor tools.</param>
/// <param name="toolCallIds">Reads the ID that the model gave the current call, and ends the loop.</param>
public sealed class AgentDraftExtractor(
    IChatClient chatClient,
    IOptions<GeminiOptions> options,
    IOptions<AgentOptions> agentOptions,
    IAgentProviderCalls providerCalls,
    ToolRegistry registry,
    IToolCallIds toolCallIds
) : IDraftExtractor
{
    public const string AgentName = "draft-extractor";

    private const string TooManyFailuresMessage = "The model made {0} invalid tool calls in a row.";

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly AgentOptions _agentOptions = agentOptions.Value;
    private readonly IAgentProviderCalls _providerCalls = providerCalls;
    private readonly ToolRegistry _registry = registry;
    private readonly IToolCallIds _toolCallIds = toolCallIds;

    public async Task ExtractAsync(
        ExtractionInput input,
        ExtractionSession session,
        CancellationToken cancellationToken
    )
    {
        var streak = new FailureStreak(_agentOptions.MaxConsecutiveErrors);
        var context = new ToolContext
        {
            CompanyId = input.CompanyId,
            RunId = session.Log.Run.Id,
            Recorder = session.Log.Recorder,
            Extraction = session.Buffer,
            Streak = streak,
            CurrentCallId = _toolCallIds.Current,
            EndLoop = _toolCallIds.EndLoop,
        };
        session.Log.Run.UseModel(_options.ChatModel, ExtractionPrompt.Version, ExtractionPrompt.HashTemplate());
        var agent = CreateAgent(ExtractionPrompt.BuildSystemPrompt(input), _registry.For(AgentRole.Extractor, context));
        var agentSession = await agent.CreateSessionAsync(cancellationToken);

        var response = await agent.RunAsync(
            ExtractionPrompt.BuildUserMessage(input),
            agentSession,
            cancellationToken: cancellationToken);

        session.Log.AddUsage(
            (int)(response.Usage?.InputTokenCount ?? 0),
            (int)(response.Usage?.OutputTokenCount ?? 0));

        if (streak.IsExceeded)
        {
            throw new InvalidExtractionException(
                string.Format(TooManyFailuresMessage, _agentOptions.MaxConsecutiveErrors));
        }
    }

    private AIAgent CreateAgent(string systemPrompt, IReadOnlyList<AIFunction> tools)
    {
        var invoking = new FunctionInvokingChatClient(new RetryingChatClient(_chatClient, _providerCalls))
        {
            MaximumIterationsPerRequest = _agentOptions.ExtractorMaxIterations,
            MaximumConsecutiveErrorsPerRequest = _agentOptions.MaxConsecutiveErrors,
        };

        return new ChatClientAgent(
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
                        Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low },
                        Tools = [.. tools],
                    },
                })
            .AsBuilder()
            .UseOpenTelemetry(
                AiServiceExtensions.SourceName,
                telemetry => telemetry.EnableSensitiveData = false)
            .Build();
    }
}
