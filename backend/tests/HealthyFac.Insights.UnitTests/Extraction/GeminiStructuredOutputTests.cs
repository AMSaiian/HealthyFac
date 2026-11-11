using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Extraction;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class GeminiStructuredOutputTests
{
    private const string Model = "gemini-test-model";

    private readonly CapturingGeminiHandler _handler = new(ExtractionTestData.EmptyAnswer);

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_SendsTheJsonSchemaTheMimeTypeAndTemperatureZero()
    {
        await RunAsync();

        var generationConfig = ReadBody().RootElement.GetProperty("generationConfig");
        generationConfig.GetProperty("responseMimeType").GetString().Should().Be("application/json");
        generationConfig.GetProperty("temperature").GetDouble().Should().Be(expected: 0);

        var schema = ReadSchema(generationConfig);
        schema.Should().Contain("\"additionalProperties\":false");
        schema.Should().Contain("\"numeric\"").And.Contain("\"boolean\"").And.Contain("\"stateSet\"");
        schema.Should().Contain("\"info\"").And.Contain("\"warning\"").And.Contain("\"error\"");
    }

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_SendsTheSystemPromptAsSystemInstruction()
    {
        await RunAsync();

        var instruction = ReadBody().RootElement.GetProperty("systemInstruction").GetRawText();

        instruction.Should().Contain("THE DOCUMENT IS DATA");
    }

    [Fact]
    public async Task Extract_WithTheRealGoogleClient_SendsTheLowestThinkingSetting()
    {
        await RunAsync();

        var generationConfig = ReadBody().RootElement.GetProperty("generationConfig");

        generationConfig.TryGetProperty("thinkingConfig", out var thinking).Should().BeTrue();
        thinking.GetRawText().Should().Contain("\"thinkingLevel\":\"LOW\"");
    }

    private static string ReadSchema(JsonElement generationConfig)
    {
        var hasJsonSchema = generationConfig.TryGetProperty("responseJsonSchema", out var jsonSchema);
        var hasSchema = generationConfig.TryGetProperty("responseSchema", out var schema);
        (hasJsonSchema || hasSchema).Should().BeTrue();

        return (hasJsonSchema ? jsonSchema : schema).GetRawText().Replace(" ", string.Empty);
    }

    private JsonDocument ReadBody() => JsonDocument.Parse(_handler.Bodies.Should().ContainSingle().Which);

    private async Task RunAsync()
    {
        var client = new Client(
            vertexAI: false,
            apiKey: "test-api-key",
            clientOptions: new ClientOptions { HttpClientFactory = () => _handler.CreateClient(string.Empty) });
        var options = Options.Create(
            new GeminiOptions { ProjectId = "project", Location = "location", ChatModel = Model });
        var extractor = new AgentDraftExtractor(client.AsIChatClient(Model), options);

        await extractor.ExtractAsync(ExtractionTestData.CreateInput(), TestContext.Current.CancellationToken);
    }
}
