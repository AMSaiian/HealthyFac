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
        var provider = CreateProvider();

        var guard = provider.GetRequiredService<IChatClient>().GetService<StallGuardChatClient>();

        guard.Should().NotBeNull();
    }

    [Fact]
    public void AddGeminiClients_ForTheAssistantChatClient_PutsTheStallGuardInTheChain()
    {
        var provider = CreateProvider();

        var guard = provider.GetRequiredKeyedService<IChatClient>(AiClientKeys.Assistant)
            .GetService<StallGuardChatClient>();

        guard.Should().NotBeNull();
    }

    [Fact]
    public void AddGeminiClients_ForTheAssistantChatClient_CallsTheAssistantModel()
    {
        var provider = CreateProvider();

        var assistant = provider.GetRequiredKeyedService<IChatClient>(AiClientKeys.Assistant)
            .GetService<ChatClientMetadata>();

        var planner = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();

        assistant!.DefaultModelId.Should().Be(expected: "assistant-model");
        planner!.DefaultModelId.Should().Be(expected: "chat-model");
    }

    private static ServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Gemini:ProjectId"] = "project",
                    ["Gemini:Location"] = "global",
                    ["Gemini:ChatModel"] = "chat-model",
                    ["Gemini:AssistantModel"] = "assistant-model",
                    ["Gemini:TriageModel"] = "triage-model",
                })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddGeminiClients(configuration)
            .BuildServiceProvider();
    }
}
