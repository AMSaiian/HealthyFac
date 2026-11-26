using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Chats.Models;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.UnitTests.Ai;

[Feature(TestFeatures.Ingest)]
[Feature(TestFeatures.Chat)]
public sealed class GeminiClientRegistrationTests
{
    [Fact]
    public void AddGeminiClients_ForTheDefaultChatClient_PutsTheStallGuardInTheChain()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Gemini:ProjectId"] = "project",
                    ["Gemini:Location"] = "global",
                    ["Gemini:ChatModel"] = "chat-model",
                    ["Gemini:TriageModel"] = "triage-model",
                })
            .Build();

        var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddGeminiClients(configuration)
            .BuildServiceProvider();

        var guard = provider.GetRequiredService<IChatClient>().GetService<StallGuardChatClient>();

        guard.Should().NotBeNull();
    }
}
