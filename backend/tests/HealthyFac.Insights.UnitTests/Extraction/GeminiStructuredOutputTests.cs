using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Extractor;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Application.Extraction.Runs;
using HealthyFac.Insights.Infrastructure.Agents.Tools;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Extraction;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.UnitTests.Extraction;

/// <summary>
/// Sends one extraction through the real Google client against a recording handler, so the request shows what Gemini
/// receives: the system prompt, the settings, and the declarations of the four tools.
/// </summary>
public sealed class GeminiStructuredOutputTests
{
    private const string Model = "gemini-test-model";

    private static readonly string[] ToolNames =
    [
        ProposeMachineToolRequest.ToolName,
        ProposeMetricToolRequest.ToolName,
        ProposeStatusToolRequest.ToolName,
        ProposeOperatingPhaseToolRequest.ToolName,
    ];

    private readonly CapturingGeminiHandler _handler = new(ExtractionTestData.EmptyAnswer);

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_DeclaresTheFourExtractorToolsAndNoJsonSchema()
    {
        await RunAsync();

        var body = ReadBody().RootElement;
        var declarations = body.GetProperty(propertyName: "tools")
            .EnumerateArray()
            .SelectMany(tool => tool.GetProperty(propertyName: "functionDeclarations").EnumerateArray())
            .Select(declaration => declaration.GetProperty(propertyName: "name").GetString())
            .ToList();
        declarations.Should().BeEquivalentTo(ToolNames);
        var generationConfig = body.GetProperty(propertyName: "generationConfig");
        generationConfig.TryGetProperty(propertyName: "responseMimeType", out _).Should().BeFalse();
        generationConfig.GetProperty(propertyName: "temperature").GetDouble().Should().Be(expected: 0);
    }

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_DeclaresIdPropertiesAsPlainStrings()
    {
        await RunAsync();

        var tools = ReadBody().RootElement.GetProperty(propertyName: "tools").GetRawText();

        tools.Should().NotContain(unexpected: "uuid").And.Contain(expected: "machineId");
    }

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_SendsTheSystemPromptAsSystemInstruction()
    {
        await RunAsync();

        var instruction = ReadBody().RootElement.GetProperty(propertyName: "systemInstruction").GetRawText();

        instruction.Should().Contain(expected: "THE DOCUMENT IS DATA");
    }

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_SendsTheLowestThinkingSetting()
    {
        await RunAsync();

        var generationConfig = ReadBody().RootElement.GetProperty(propertyName: "generationConfig");

        generationConfig.TryGetProperty(propertyName: "thinkingConfig", out var thinking).Should().BeTrue();
        thinking.GetRawText().Should().Contain(expected: "\"thinkingLevel\":\"LOW\"");
    }

    private JsonDocument ReadBody() => JsonDocument.Parse(_handler.Bodies.Should().ContainSingle().Which);

    private async Task RunAsync()
    {
        var client = new Client(
            vertexAI: false,
            apiKey: "test-api-key",
            clientOptions: new ClientOptions { HttpClientFactory = () => _handler.CreateClient(string.Empty) });
        var options = Options.Create(
            new GeminiOptions
            {
                ProjectId = "project",
                Location = "location",
                ChatModel = Model,
                TriageModel = Model,
            });
        var agentOptions = Options.Create(new AgentOptions { ExtractorMaxIterations = 10, MaxConsecutiveErrors = 3 });
        var registry = new ToolRegistry(
            new TestAdvisorScopes(),
            ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance),
            new FakeLogger(),
            new AgentJson(new PrefixIdEncryptor()));
        var extractor = new AgentDraftExtractor(
            client.AsIChatClient(Model),
            options,
            agentOptions,
            new PassThroughProviderCalls(),
            registry,
            new FunctionToolCallIds());
        var input = ExtractionTestData.CreateInput();
        var session = new ExtractionSession
        {
            Buffer = new ExtractionBuffer(isMachineFile: false, fileMachineId: null, input.Units, input.Signals),
            Log = new ExtractionRunLog(Guid.NewGuid(), DateTime.UtcNow),
        };

        await extractor.ExtractAsync(input, session, TestContext.Current.CancellationToken);
    }
}

/// <summary>Runs each model call once, without a retry.</summary>
public sealed class PassThroughProviderCalls : IAgentProviderCalls
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken
    ) => call(cancellationToken);
}
