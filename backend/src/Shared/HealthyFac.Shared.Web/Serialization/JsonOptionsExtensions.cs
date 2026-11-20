using System.Text.Json;
using System.Text.Json.Serialization;
using HealthyFac.Shared.Application.Serialization;
using HealthyFac.Shared.Infrastructure.TextInput;
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
        /// readable and stable, so a reordered enum never changes the payload. Every string of a request is normalized
        /// on read (<see cref="TextInputJson"/>).
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
    public void Configure(JsonOptions options) => Apply(options.SerializerOptions);

    /// <summary>
    /// Adds the shared converters to the given serializer options. The HTTP options and the hub protocol options of
    /// the real-time hub both call it, so the two wire formats stay equal.
    /// </summary>
    /// <param name="options">The serializer options to change.</param>
    public static void Apply(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new EnumerationJsonConverter());
        TextInputJson.Configure(options);
    }
}
