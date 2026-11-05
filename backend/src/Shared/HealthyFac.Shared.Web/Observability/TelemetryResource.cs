using System.Reflection;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using Attributes = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Resource.Attributes;
using ResourceConstants = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Resource;

namespace HealthyFac.Shared.Web.Observability;

/// <summary>
/// Defines the one set of resource attributes that traces, metrics, and logs carry, so a query can join the three
/// signals of one service instance. Platform attributes (<c>k8s.*</c>, <c>host.*</c>) come from the deployment or
/// the collector, never from here.
/// </summary>
public static class TelemetryResource
{
    /// <summary>
    /// One ID per process. Every signal carries the same value, so the collector keeps the series of two replicas
    /// apart.
    /// </summary>
    private static readonly string InstanceId = Guid.NewGuid().ToString();

    /// <summary>
    /// Creates the resource attributes of the running service from <see cref="ServiceOptions"/> and the host
    /// environment. The version is the informational version of the application assembly (the entry assembly of a
    /// real host). The attribute is absent when the assembly has none.
    /// </summary>
    /// <param name="services">The service provider that holds <see cref="ServiceOptions"/> and
    /// <see cref="IHostEnvironment"/>.</param>
    /// <returns>The attributes by their OpenTelemetry key.</returns>
    public static IReadOnlyDictionary<string, object> CreateAttributes(IServiceProvider services)
    {
        var serviceOptions = services.GetRequiredService<IOptions<ServiceOptions>>().Value;
        var environment = services.GetRequiredService<IHostEnvironment>();
        var version = Assembly.Load(new AssemblyName(environment.ApplicationName))
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var attributes = new Dictionary<string, object>
        {
            [Attributes.ServiceNamespace] = ResourceConstants.Namespace,
            [Attributes.ServiceName] = serviceOptions.Name,
            [Attributes.ServiceInstanceId] = InstanceId,
            [Attributes.DeploymentEnvironment] = environment.EnvironmentName.ToLowerInvariant(),
        };

        if (version is not null)
        {
            attributes[Attributes.ServiceVersion] = version;
        }

        return attributes;
    }

    extension(ResourceBuilder resource)
    {
        /// <summary>
        /// Adds the shared attributes of <see cref="CreateAttributes"/>, then the SDK environment variable detector
        /// again.
        /// </summary>
        /// <remarks>
        /// The builder merges in order, and a later value wins. The default builder reads <c>OTEL_SERVICE_NAME</c>
        /// and <c>OTEL_RESOURCE_ATTRIBUTES</c> first, so the configured values would hide them. The second detector
        /// lets a deployment override any attribute.
        /// </remarks>
        /// <returns>The same <see cref="ResourceBuilder"/> for chaining.</returns>
        public ResourceBuilder AddSharedAttributes() =>
            resource
                .AddDetector(provider => new SharedAttributesDetector(CreateAttributes(provider)))
                .AddEnvironmentVariableDetector();
    }

    private sealed class SharedAttributesDetector(IReadOnlyDictionary<string, object> attributes) : IResourceDetector
    {
        private readonly IReadOnlyDictionary<string, object> _attributes = attributes;

        public Resource Detect() => new(_attributes);
    }
}
