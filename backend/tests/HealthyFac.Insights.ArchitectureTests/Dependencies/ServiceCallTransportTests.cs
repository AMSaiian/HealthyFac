using HealthyFac.Insights.Infrastructure;
using NetArchTest.Rules;

namespace HealthyFac.Insights.ArchitectureTests.Dependencies;

/// <summary>
/// A HealthyFac service calls another HealthyFac service only over gRPC. HTTP stays for the web app and for external
/// systems that offer no gRPC. The test lists the types that call an external system over HTTP.
/// </summary>
public sealed class ServiceCallTransportTests
{
    private const string HttpNamespace = "System.Net.Http";

    /// <summary>The types that call an external system (Docling, the AI provider, a blob link) over HTTP.</summary>
    private static readonly string[] ExternalHttpCallers =
    [
        "HealthyFac.Insights.Infrastructure.Ai.AiServiceExtensions",
        "HealthyFac.Insights.Infrastructure.Conversion.DoclingExtensions",
        "HealthyFac.Insights.Infrastructure.Conversion.DoclingHealthCheck",
        "HealthyFac.Insights.Infrastructure.Conversion.DoclingDocumentConverter",
        "HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls.HttpBlobStreamSource",
        "HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls.ProviderFailures",
        "HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls.ResponseBodyStream",
        "HealthyFac.Insights.Infrastructure.Ingest.Worker.IngestWorkerExtensions",
    ];

    [Fact]
    public void InfrastructureTypes_OutsideTheExternalCallers_UseNoHttpClient()
    {
        var result = Types.InAssembly(InsightsInfrastructureAssembly.Instance)
            .ShouldNot()
            .HaveDependencyOnAny(HttpNamespace)
            .GetResult();

        var failing = (result.FailingTypes ?? [])
            .Select(type => type.FullName!.Split(separator: '/')[0])
            .Where(name => !ExternalHttpCallers.Contains(name));

        failing.Should().BeEmpty();
    }
}
