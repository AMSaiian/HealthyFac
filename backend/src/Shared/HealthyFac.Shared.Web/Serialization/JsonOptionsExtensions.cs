using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.Serialization;

/// <summary>
/// Provides extension methods for the shared JSON serialization conventions.
/// </summary>
public static class JsonOptionsExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the shared JSON conventions. Native enums serialize as camelCase strings rather than
        /// numbers, and enumeration members serialize as their own value. Both keep the wire contract
        /// readable and stable, so a reordered enum never changes the payload.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedJsonOptions()
        {
            services.AddTransient<IConfigureOptions<JsonOptions>, SharedJsonOptionsConfigurator>();

            return services;
        }
    }
}

internal sealed class SharedJsonOptionsConfigurator : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        options.SerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.SerializerOptions.Converters.Add(new EnumerationJsonConverter());
    }
}
