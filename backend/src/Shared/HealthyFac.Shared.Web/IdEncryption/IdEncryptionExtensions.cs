using HealthyFac.Shared.Infrastructure.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption;
using HealthyFac.Shared.Web.IdEncryption.Converters;

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
            services.AddValidatedOptions<IdEncryptionOptions, IdEncryptionOptionsValidator>(
                IdEncryptionOptions.SectionName,
                configuration
            );

            services.AddSingleton<IIdEncryptor, AesKwIdEncryptor>();
            services.AddOptions();
            services.AddTransient<IConfigureOptions<JsonOptions>, IdEncryptionJsonOptionsConfigurator>();

            return services;
        }
    }

    /// <summary>
    /// Applies ID encryption converters to JSON object properties matching the ID naming convention.
    /// </summary>
    public static void ApplyIdEncryption(JsonTypeInfo typeInfo, IIdEncryptor encryptor)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (!property.Name.EndsWith(
                    IdEncryptionConstants.Conventions.PropertySuffix,
                    StringComparison.OrdinalIgnoreCase)
                && !property.Name.Equals(
                    IdEncryptionConstants.Conventions.ExactPropertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hasIgnore = property.AttributeProvider?
                    .GetCustomAttributes(typeof(IgnoreIdEncryptionAttribute), false)
                    .Length
                > 0;

            if (hasIgnore)
            {
                continue;
            }

            var converter = ResolveConverter(property.PropertyType, encryptor);
            if (converter is not null)
            {
                property.CustomConverter = converter;
            }
        }
    }

    private static JsonConverter? ResolveConverter(Type propertyType, IIdEncryptor encryptor)
    {
        return propertyType switch
        {
            not null when propertyType == typeof(Guid) => new EncryptedGuidConverter(encryptor),
            not null when propertyType == typeof(Guid?) => new EncryptedNullableGuidConverter(encryptor),
            _ => null,
        };
    }
}

internal sealed class IdEncryptionJsonOptionsConfigurator(IIdEncryptor encryptor) : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        var resolver = options.SerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.SerializerOptions.TypeInfoResolver = resolver.WithAddedModifier(typeInfo =>
            IdEncryptionExtensions.ApplyIdEncryption(typeInfo, encryptor)
        );
    }
}
