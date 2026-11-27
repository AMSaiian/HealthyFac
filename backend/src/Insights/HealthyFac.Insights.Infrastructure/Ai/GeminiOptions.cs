using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>The Google Cloud project that Vertex AI bills.</summary>
    public required string ProjectId { get; init; }

    /// <summary>The Vertex AI location, for example <c>europe-west4</c>.</summary>
    public required string Location { get; init; }

    /// <summary>
    /// The Vertex AI API version. The SDK defaults to <c>v1beta1</c> for Vertex AI. The models and the features that
    /// the app uses are generally available on <c>v1</c>. Long decoding with a continuation token exists only on
    /// <c>v1beta1</c>, and the app does not use it.
    /// </summary>
    public string ApiVersion { get; init; } = "v1";

    /// <summary>The model of the planner and the extractor.</summary>
    public required string ChatModel { get; init; }

    /// <summary>The model of the chat answer. The planner and the extractor use <see cref="ChatModel"/>.</summary>
    public required string AssistantModel { get; init; }

    /// <summary>The small model of the triage and the screener, for example a Flash-Lite model.</summary>
    public required string TriageModel { get; init; }

    public string EmbeddingModel { get; init; } = "gemini-embedding-001";

    /// <summary>The size of an embedding. It must equal the size of the <c>vector</c> column.</summary>
    public int EmbeddingDimensions { get; init; } = 1536;

    /// <summary>The most tokens of one model answer.</summary>
    public int MaxOutputTokens { get; init; } = 32768;

    /// <summary>The most embedding calls that run at the same time.</summary>
    public int EmbeddingParallelism { get; init; } = 4;

    /// <summary>
    /// The longest wait for the next update of a chat model stream, in seconds. A longer silence counts as a stall.
    /// </summary>
    public int StreamIdleTimeoutSeconds { get; init; } = 30;
}

public sealed class GeminiOptionsValidator : AbstractValidator<GeminiOptions>
{
    public GeminiOptionsValidator()
    {
        RuleFor(options => options.ProjectId).NotEmpty().WithMessage(GeminiMessages.ProjectIdRequired);
        RuleFor(options => options.Location).NotEmpty().WithMessage(GeminiMessages.LocationRequired);
        RuleFor(options => options.ApiVersion).NotEmpty().WithMessage(GeminiMessages.ApiVersionRequired);
        RuleFor(options => options.ChatModel).NotEmpty().WithMessage(GeminiMessages.ChatModelRequired);
        RuleFor(options => options.AssistantModel).NotEmpty().WithMessage(GeminiMessages.AssistantModelRequired);
        RuleFor(options => options.TriageModel).NotEmpty().WithMessage(GeminiMessages.TriageModelRequired);
        RuleFor(options => options.EmbeddingModel).NotEmpty().WithMessage(GeminiMessages.EmbeddingModelRequired);

        RuleFor(options => options.EmbeddingDimensions)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(GeminiMessages.EmbeddingDimensionsInvalid);

        RuleFor(options => options.MaxOutputTokens)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(GeminiMessages.MaxOutputTokensInvalid);

        RuleFor(options => options.EmbeddingParallelism)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(GeminiMessages.EmbeddingParallelismInvalid);

        RuleFor(options => options.StreamIdleTimeoutSeconds)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(GeminiMessages.StreamIdleTimeoutInvalid);
    }
}
