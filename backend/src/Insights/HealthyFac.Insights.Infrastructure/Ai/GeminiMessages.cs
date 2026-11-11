namespace HealthyFac.Insights.Infrastructure.Ai;

public static class GeminiMessages
{
    public const string ProjectIdRequired = "Gemini:ProjectId is required.";
    public const string LocationRequired = "Gemini:Location is required.";
    public const string ChatModelRequired = "Gemini:ChatModel is required.";
    public const string EmbeddingModelRequired = "Gemini:EmbeddingModel is required.";
    public const string EmbeddingDimensionsInvalid = "Gemini:EmbeddingDimensions must be above 0.";
    public const string MaxOutputTokensInvalid = "Gemini:MaxOutputTokens must be above 0.";
    public const string EmbeddingParallelismInvalid = "Gemini:EmbeddingParallelism must be above 0.";
}
