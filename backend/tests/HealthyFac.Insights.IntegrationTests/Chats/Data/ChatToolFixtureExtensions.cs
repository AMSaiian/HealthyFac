using System.Reflection;
using System.Text.Json;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// Calls a chat tool the way a turn does: through the registry, in the request scope of a user, with a stored chat run.
/// </summary>
public static class ChatToolFixtureExtensions
{
    extension(ChatTurnFixture fixture)
    {
        /// <summary>
        /// Calls the tool with the permissions of every chat tool and returns its result text and the run that logged
        /// the call.
        /// </summary>
        public async Task<(string Result, Guid RunId)> CallChatToolAsync(string toolName, string argumentsJson)
        {
            var now = fixture.Time.GetUtcNow().UtcDateTime;
            var run = new AgentRun(AgentRole.Chat, caseId: null, ingestAttemptId: null, now);
            run.Claim(now, now.AddMinutes(value: 1));
            using var arrangeScope = fixture.Inner.ScopeFactory.CreateScope();
            var arrangeContext = arrangeScope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            arrangeContext.AgentRuns.Add(run);
            await arrangeContext.SaveChangesAsync();

            using var scope = fixture.Inner.CreateActorScope(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = ChatTestData.OwnerId,
                    CompanyId = ChatTestData.CompanyId,
                });

            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            var context = new ToolContext
            {
                CompanyId = ChatTestData.CompanyId,
                RunId = run.Id,
                Recorder = ToolCallRecorder.ForDatabase(dbContext, run.Id),
            };

            var permissions = ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance)
                .SelectMany(type => type.GetCustomAttribute<AgentToolAttribute>()!.Permissions)
                .ToHashSet();

            var function = fixture.Inner.GetSingleton<ToolRegistry>()
                .For(AgentRole.Chat, context, permissions, new ChatToolScopes(scope.ServiceProvider))
                .Single(candidate => candidate.Name == toolName);

            var arguments = new AIFunctionArguments(
                JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson));

            var result = await function.InvokeAsync(arguments);

            return (result!.ToString()!, run.Id);
        }

        public async Task<ChatToolSession> StartToolSessionAsync()
        {
            var now = fixture.Time.GetUtcNow().UtcDateTime;
            var run = new AgentRun(AgentRole.Chat, caseId: null, ingestAttemptId: null, now);
            run.Claim(now, now.AddMinutes(value: 1));
            using var arrangeScope = fixture.Inner.ScopeFactory.CreateScope();
            var arrangeContext = arrangeScope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            arrangeContext.AgentRuns.Add(run);
            await arrangeContext.SaveChangesAsync();

            return new ChatToolSession(fixture, run.Id);
        }

        public async Task<IReadOnlyList<AgentToolCall>> ReadToolCallsAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentToolCalls
                .Where(call => call.RunId == runId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<AgentToolCall>> ReadAllToolCallsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentToolCalls
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TMessage>> ReadOutboxAsync<TMessage>()
            where TMessage : HealthyFac.Shared.Application.Messaging.IIntegrationMessage
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;

            var payloads = await scope.ServiceProvider
                .GetRequiredService<HealthyFac.Insights.Infrastructure.Database.InsightsDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return
            [
                .. payloads.Select(payload => JsonSerializer.Deserialize<TMessage>(
                    payload, Shared.Infrastructure.Messaging.MessagingJsonOptions.Instance)!),
            ];
        }

        public async Task<int> CountRunsAsync(Guid runId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IInsightsDbContext>()
                .AgentRuns
                .CountAsync(run => run.Id == runId);
        }
    }
}
