using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.Web.Configuration;

/// <summary>
/// Adds the committed seed data file to the configuration of a service.
/// </summary>
public static class SeedDataExtensions
{
    public const string RelativePath = "seed/seed.Development.json";

    extension(IConfigurationBuilder configuration)
    {
        /// <summary>
        /// In the Development environment, adds <see cref="RelativePath"/> before the first environment variable
        /// source, so an environment variable still overrides a seed value. Other environments read no seed file.
        /// </summary>
        /// <remarks>
        /// The file is required in Development: a missing mount would otherwise seed without the machines and show
        /// no error.
        /// </remarks>
        /// <param name="environment">The host environment.</param>
        /// <param name="baseDirectory">
        /// The directory that holds <c>seed/</c>. The default is the application base.
        /// </param>
        /// <returns>The <see cref="IConfigurationBuilder"/> instance for chained configuration.</returns>
        public IConfigurationBuilder AddSeedDataFile(IHostEnvironment environment, string? baseDirectory = null)
        {
            if (!environment.IsDevelopment())
            {
                return configuration;
            }

            var source = new JsonConfigurationSource
            {
                Path = RelativePath,
                Optional = false,
                FileProvider = new PhysicalFileProvider(baseDirectory ?? AppContext.BaseDirectory),
            };
            source.ResolveFileProvider();

            var environmentIndex = configuration.Sources.ToList()
                .FindIndex(existing => existing is EnvironmentVariablesConfigurationSource);

            configuration.Sources.Insert(
                environmentIndex < 0 ? configuration.Sources.Count : environmentIndex,
                source);

            return configuration;
        }
    }
}
