using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.IdEncryption;

public static class IdEncryptionServiceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated ID encryption options and the encryptor. A service with an HTTP API also adds the
        /// JSON converters (<c>AddIdEncryption</c> in Shared.Web).
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> instance to read the encryption section from. Pass
        /// <see langword="null"/> to read it from the configuration resolved out of the service provider.
        /// </param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddIdEncryptor(IConfiguration? configuration = null)
        {
            services.AddValidatedOptions<IdEncryptionOptions, IdEncryptionOptionsValidator>(
                IdEncryptionOptions.SectionName,
                configuration
            );
            services.AddSingleton<IIdEncryptor, AesKwIdEncryptor>();

            return services;
        }
    }
}
