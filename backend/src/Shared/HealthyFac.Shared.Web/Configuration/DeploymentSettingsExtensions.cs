using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace HealthyFac.Shared.Web.Configuration;

/// <summary>
/// Adds the settings file of the place where a service runs (e.g., the Compose network) to its configuration.
/// </summary>
public static class DeploymentSettingsExtensions
{
    public const string FileName = "appsettings.Deployment.json";

    extension(IConfigurationBuilder configuration)
    {
        /// <summary>
        /// Adds <see cref="FileName"/> above <c>appsettings.json</c> and the environment file, and below the
        /// environment variables. The file overrides both appsettings files, and an environment variable still
        /// overrides the file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The file goes before the first environment variable source that has no prefix. The host builder adds
        /// its prefixed sources (<c>DOTNET_</c>, <c>ASPNETCORE_</c>) before <c>appsettings.json</c>, so a place
        /// before those would put the file below the appsettings files.
        /// </para>
        /// <para>
        /// The file is optional. A deployment mounts it next to the application; a run on the host has none.
        /// It holds the fixed values of that deployment that are no secrets (e.g., the addresses inside its
        /// network). A secret, and a value that the deployment builds from its own variables, stays an
        /// environment variable.
        /// </para>
        /// </remarks>
        /// <param name="baseDirectory">
        /// The directory that holds the file. The default is the application base.
        /// </param>
        /// <returns>The <see cref="IConfigurationBuilder"/> instance for chained configuration.</returns>
        public IConfigurationBuilder AddDeploymentSettingsFile(string? baseDirectory = null)
        {
            var source = new JsonConfigurationSource
            {
                Path = FileName,
                Optional = true,
                FileProvider = new PhysicalFileProvider(baseDirectory ?? AppContext.BaseDirectory),
            };
            source.ResolveFileProvider();

            var environmentIndex = configuration.Sources.ToList()
                .FindIndex(existing => existing is EnvironmentVariablesConfigurationSource { Prefix: null or "" });

            configuration.Sources.Insert(
                environmentIndex < 0 ? configuration.Sources.Count : environmentIndex,
                source);

            return configuration;
        }
    }
}
