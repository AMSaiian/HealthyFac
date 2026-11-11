using FluentValidation;

namespace HealthyFac.Insights.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>The Google Cloud project that Vertex AI bills.</summary>
    public required string ProjectId { get; init; }

    /// <summary>The Vertex AI location, for example <c>europe-west4</c>.</summary>
    public required string Location { get; init; }

    public required string ChatModel { get; init; }

    public string EmbeddingModel { get; init; } = "gemini-embedding-001";

    /// <summary>The size of an embedding. It must equal the size of the <c>vector</c> column.</summary>
    public int EmbeddingDimensions { get; init; } = 1536;

    /// <summary>The most tokens of one model answer.</summary>
    public int MaxOutputTokens { get; init; } = 32768;

    /// <summary>The most embedding calls that run at the same time.</summary>
    public int EmbeddingParallelism { get; init; } = 4;
}

public sealed class GeminiOptionsValidator : AbstractValidator<GeminiOptions>
{
    public GeminiOptionsValidator()
    {
        RuleFor(options => options.ProjectId).NotEmpty().WithMessage(GeminiMessages.ProjectIdRequired);
        RuleFor(options => options.Location).NotEmpty().WithMessage(GeminiMessages.LocationRequired);
        RuleFor(options => options.ChatModel).NotEmpty().WithMessage(GeminiMessages.ChatModelRequired);
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
    }
}
