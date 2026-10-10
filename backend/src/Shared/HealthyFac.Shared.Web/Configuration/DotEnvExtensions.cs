using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Shared.Web.Configuration;

/// <summary>
/// Provides extension methods for loading a dotenv file into the process environment.
/// </summary>
public static class DotEnvExtensions
{
    extension(IConfigurationBuilder configuration)
    {
        /// <summary>
        /// Loads the nearest <c>.env</c> file into the process environment, then re-reads environment
        /// variables so the loaded values reach configuration.
        /// </summary>
        /// <returns>The <see cref="IConfigurationBuilder"/> instance for chained configuration.</returns>
        public IConfigurationBuilder AddDotEnvFile()
        {
            DotEnvLoader.Load();

            return configuration.AddEnvironmentVariables();
        }
    }
}
