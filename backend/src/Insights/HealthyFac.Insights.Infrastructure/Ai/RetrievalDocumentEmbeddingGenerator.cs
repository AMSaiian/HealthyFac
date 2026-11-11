using Google.GenAI.Types;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Infrastructure.Ai;

/// <summary>
/// Sets the task type <c>RETRIEVAL_DOCUMENT</c> on every embedding call, so the vectors fit a search over stored
/// document chunks. The Google type stays in Infrastructure; the Application layer sees only the M.E.AI generator.
/// </summary>
/// <remarks>
/// The wrapper of Google.GenAI reads the raw <see cref="EmbedContentConfig"/> from
/// <see cref="EmbeddingGenerationOptions.RawRepresentationFactory"/>, and the size from
/// <see cref="EmbeddingGenerationOptions.Dimensions"/>.
/// </remarks>
/// <param name="inner">The Google.GenAI generator.</param>
public sealed class RetrievalDocumentEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> inner)
    : DelegatingEmbeddingGenerator<string, Embedding<float>>(inner)
{
    public const string TaskType = "RETRIEVAL_DOCUMENT";

    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var taskOptions = options is null ? new EmbeddingGenerationOptions() : options.Clone();
        taskOptions.RawRepresentationFactory = _ => new EmbedContentConfig
        {
            TaskType = TaskType,
            OutputDimensionality = taskOptions.Dimensions,
        };

        return base.GenerateAsync(values, taskOptions, cancellationToken);
    }
}
