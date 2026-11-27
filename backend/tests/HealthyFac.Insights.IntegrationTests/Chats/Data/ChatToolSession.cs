using System.Reflection;
using System.Text.Json;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// One request scope of a chat turn that calls several tools in a row, so the tools share the draft buffer of the
/// turn like the tools of a real turn do.
/// </summary>
public sealed class ChatToolSession : IDisposable
{
    private readonly IServiceScope _scope;
    private readonly ChatTurnFixture _fixture;

    public ChatToolSession(ChatTurnFixture fixture, Guid runId)
    {
        _fixture = fixture;
        RunId = runId;

        _scope = fixture.Inner.CreateActorScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = ChatTestData.OwnerId,
                CompanyId = ChatTestData.CompanyId,
            });
    }

    public Guid RunId { get; }

    public ChatDraftBuffer Buffer => _scope.ServiceProvider.GetRequiredService<ChatDraftBuffer>();

    public async Task<string> CallAsync(string toolName, string argumentsJson)
    {
        var dbContext = _scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

        var context = new ToolContext
        {
            CompanyId = ChatTestData.CompanyId,
            RunId = RunId,
            Recorder = ToolCallRecorder.ForDatabase(dbContext, RunId),
        };

        var permissions = ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance)
            .SelectMany(type => type.GetCustomAttribute<AgentToolAttribute>()!.Permissions)
            .ToHashSet();

        var function = _fixture.Inner.GetSingleton<ToolRegistry>()
            .For(AgentRole.Chat, context, permissions, new ChatToolScopes(_scope.ServiceProvider))
            .Single(candidate => candidate.Name == toolName);

        var arguments = new AIFunctionArguments(JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson));

        var result = await function.InvokeAsync(arguments);

        return result!.ToString()!;
    }

    public void Dispose() => _scope.Dispose();
}
