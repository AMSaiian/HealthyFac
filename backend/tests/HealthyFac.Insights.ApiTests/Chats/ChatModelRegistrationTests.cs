using HealthyFac.Insights.ApiTests.Chats.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Chats;

[Collection(InsightsApiCollection.Name)]
[Feature(TestFeatures.Chat)]
public sealed class ChatModelRegistrationTests(InsightsApiFixture fixture)
{
    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task ChatModel_ForAChatAnswer_CallsTheKeyedAssistantClientWithTheAssistantModel()
    {
        var assistant = new RecordingAssistantChatClient();

        await using var factory = _fixture.CreateFactory(services =>
            services.AddKeyedSingleton<IChatClient>(AiClientKeys.Assistant, assistant));

        var model = factory.Services.GetRequiredService<IChatModel>();
        var tools = ChatTurnTools.Create([], maxCalls: 1, () => { }, _ => { });

        var history = new List<ChatPromptMessage>
        {
            new() { Role = ChatMessageRole.User, Text = "Which machines do we have?" },
        };

        var chunks = await model.StreamAsync(
                systemPrompt: "system prompt", history, tools, TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        assistant.StreamingCallCount.Should().Be(expected: 1);
        model.ModelName.Should().Be(expected: "test-assistant-model");
        chunks.Select(chunk => chunk.Text).Should().Contain(RecordingAssistantChatClient.AnswerText);
    }
}
