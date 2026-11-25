using System.Text.Json;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Agents.Models;
using HealthyFac.Insights.Infrastructure.Agents.Prompts;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Chats.Models;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatTriageModelTests
{
    private readonly FakeTimeProvider _time = new(AgentProviderCallsTestData.Start);
    private readonly ScriptedChatClient _client = new();
    private readonly GeminiChatTriageModel _model;

    public ChatTriageModelTests()
    {
        _model = new GeminiChatTriageModel(
            _client,
            Options.Create(
                new GeminiOptions
                {
                    ProjectId = "project",
                    Location = "location",
                    ChatModel = "chat-model",
                    TriageModel = "triage-model",
                }),
            new EmbeddedAgentPrompts(),
            new AgentProviderCalls(
                Options.Create(new AgentOptions { ProviderRetryDelaysSeconds = AgentProviderCallsTestData.Delays }),
                _time));
    }

    [Fact]
    public async Task AskAsync_WithTextAndLabels_SendsOnlyThemInOneDataBlockAndNoTools()
    {
        _client.ScriptText(text: """{"verdict":"inScope"}""");

        await _model.AskAsync(
            ChatTriageModelTestData.Text, ChatTriageModelTestData.Labels, TestContext.Current.CancellationToken);

        var message = _client.SentMessages.Should().ContainSingle().Subject.Should().ContainSingle().Subject;
        var json = message.Text.Split(separator: '\n', StringSplitOptions.TrimEntries)[1];
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToList();
        properties.Should().BeEquivalentTo("text", "linkLabels");
        document.RootElement.GetProperty(propertyName: "text").GetString().Should().Be(ChatTriageModelTestData.Text);

        document.RootElement.GetProperty(propertyName: "linkLabels")
            .EnumerateArray()
            .Select(label => label.GetString())
            .Should()
            .Equal(ChatTriageModelTestData.Labels);

        _client.SentOptions.Single()!.Tools.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ChatTriageModelTestData.Verdicts), MemberType = typeof(ChatTriageModelTestData))]
    public async Task AskAsync_WithAnAnswer_MapsTheVerdict(string caseName, string answer, ChatTriageChoice expected)
    {
        _client.ScriptText(answer);

        var result = await _model.AskAsync(
            ChatTriageModelTestData.Text, ChatTriageModelTestData.Labels, TestContext.Current.CancellationToken);

        result.Verdict.Verdict.Should().Be(expected, caseName);
    }

    [Theory]
    [MemberData(nameof(ChatTriageModelTestData.BrokenAnswers), MemberType = typeof(ChatTriageModelTestData))]
    public async Task AskAsync_WithABrokenAnswer_ThrowsProviderUnavailable(string caseName, string answer)
    {
        _client.ScriptText(answer);

        var act = () => _model.AskAsync(
            ChatTriageModelTestData.Text, ChatTriageModelTestData.Labels, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>(caseName);
    }

    [Fact]
    public async Task AskAsync_Always_SendsASchemaWithTheCamelCaseVerdicts()
    {
        _client.ScriptText(text: """{"verdict":"inScope"}""");

        await _model.AskAsync(
            ChatTriageModelTestData.Text, ChatTriageModelTestData.Labels, TestContext.Current.CancellationToken);

        var format = _client.SentOptions.Single()!.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>().Subject;

        var values = format.Schema!.Value.GetProperty(propertyName: "properties")
            .GetProperty(propertyName: "verdict")
            .GetProperty(propertyName: "enum")
            .EnumerateArray()
            .Select(value => value.GetString())
            .ToList();

        values.Should().BeEquivalentTo("inScope", "offTopic", "malicious");
    }

    [Fact]
    public async Task AskAsync_WithATransientErrorEveryTime_ThrowsProviderUnavailableAfterTheRetries()
    {
        _client.Failure = AgentProviderCallsTestData.Transient();

        var task = _model.AskAsync(
            ChatTriageModelTestData.Text, ChatTriageModelTestData.Labels, TestContext.Current.CancellationToken);

        for (var step = 0; step < AgentProviderCallsTestData.MaxSteps && !task.IsCompleted; step++)
        {
            _time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.Yield();
        }

        var act = () => task;
        await act.Should().ThrowAsync<ProviderUnavailableException>();
        _client.CallCount.Should().Be(expected: 4);
    }
}
