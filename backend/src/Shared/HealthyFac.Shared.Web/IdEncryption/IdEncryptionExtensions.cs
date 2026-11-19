using System.Text.Json.Serialization.Metadata;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.IdEncryption;

public static class IdEncryptionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers identifier encryption: validated options, the encryptor, and the JSON converters that
        /// encrypt identifier properties on the wire.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> instance to read the encryption section from. Pass
        /// <see langword="null"/> to read it from the configuration resolved out of the service provider.
        /// </param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddIdEncryption(IConfiguration? configuration = null)
        {
            services.AddIdEncryptor(configuration);
            services.AddOptions();
            services.AddTransient<IConfigureOptions<JsonOptions>, IdEncryptionJsonOptionsConfigurator>();

            return services;
        }
    }
}

internal sealed class IdEncryptionJsonOptionsConfigurator(IIdEncryptor encryptor) : IConfigureOptions<JsonOptions>
{
    private readonly IIdEncryptor _encryptor = encryptor;

    public void Configure(JsonOptions options)
    {
        var resolver = options.SerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.SerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(typeInfo =>
            IdEncryptionJsonModifier.Apply(typeInfo, _encryptor)
        );
    }
}
