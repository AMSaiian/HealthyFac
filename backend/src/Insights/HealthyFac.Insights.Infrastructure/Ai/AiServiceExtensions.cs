using Google.GenAI;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Ai;

/// <summary>
/// Provides extension methods for registering the Gemini clients.
/// </summary>
public static class AiServiceExtensions
{
    /// <summary>The name of the activity source and the meter of the model calls.</summary>
    public const string SourceName = ObservabilityConstants.Ingest.Ai;

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Gemini options, one Google.GenAI client for Vertex AI (the project, the location,
        /// and the Application Default Credentials from <c>GOOGLE_APPLICATION_CREDENTIALS</c>), the embedding
        /// generator with the task type <c>RETRIEVAL_DOCUMENT</c>, the chat client, and the embedder. Each
        /// generator and client writes OpenTelemetry data without the prompt and answer text.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddGeminiClients(IConfiguration configuration)
        {
            services.AddValidatedOptions<GeminiOptions, GeminiOptionsValidator>(
                GeminiOptions.SectionName,
                configuration);

            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;

                return new Client(vertexAI: true, project: options.ProjectId, location: options.Location);
            });

            services.AddEmbeddingGenerator(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;

                    return provider.GetRequiredService<Client>()
                        .AsIEmbeddingGenerator(options.EmbeddingModel, options.EmbeddingDimensions);
                })
                .Use(inner => new RetrievalDocumentEmbeddingGenerator(inner))
                .UseOpenTelemetry(
                    sourceName: SourceName,
                    configure: telemetry => telemetry.EnableSensitiveData = false);

            services.AddChatClient(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;

                    return provider.GetRequiredService<Client>().AsIChatClient(options.ChatModel);
                })
                .UseOpenTelemetry(
                    sourceName: SourceName,
                    configure: telemetry => telemetry.EnableSensitiveData = false);

            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;

                return new ChunkEmbeddingLimits
                {
                    Dimensions = options.EmbeddingDimensions,
                    Parallelism = options.EmbeddingParallelism,
                };
            });
            services.AddSingleton<ChunkEmbedder>();

            return services;
        }
    }
}
