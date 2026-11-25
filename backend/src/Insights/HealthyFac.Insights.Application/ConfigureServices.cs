using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.Extraction.Mapping;
using HealthyFac.Insights.Application.Ingest.Processing.Monitoring;
using HealthyFac.Insights.Application.Ingest.Processing.Pipeline;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application;

/// <summary>
/// Registers application layer services into the dependency injection container.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the use cases (handlers, decorators, and validators) and the services that they share.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApplicationServices()
        {
            services.AddUseCases(InsightsApplicationAssembly.Instance);
            services.AddScoped<IngestPipeline>();
            services.AddSingleton<IngestMetrics>();
            services.AddSingleton<DraftProposalMapper>();
            services.AddScoped<RunCommit>();
            services.AddScoped<IChatTurnSaves, ChatTurnSaves>();
            services.AddScoped<ChatTurnRunner>();
            services.AddSingleton<AgentMetrics>();

            services.AddSingleton(provider => new ToolRegistry(
                provider.GetRequiredService<IAdvisorScopes>(),
                ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance),
                provider.GetRequiredService<ILogger<ToolRegistry>>(),
                provider.GetRequiredService<IAgentJson>()));

            return services;
        }
    }
}
