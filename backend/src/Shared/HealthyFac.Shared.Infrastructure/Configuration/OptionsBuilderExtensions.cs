using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Infrastructure.Configuration;

public static class OptionsBuilderExtensions
{
    extension<TOptions>(OptionsBuilder<TOptions> builder) where TOptions : class
    {
        /// <summary>
        /// Adds FluentValidation enforcement to the OptionsBuilder pipeline.
        /// </summary>
        public OptionsBuilder<TOptions> AddConfigurationValidation()
        {
            builder.Services.AddSingleton<IValidateOptions<TOptions>>(sp => new ConfigurationValidator<TOptions>(
                    builder.Name,
                    sp.GetRequiredService<IValidator<TOptions>>()
                )
            );

            return builder;
        }
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a strongly-typed options configuration bound to a section, validated by
        /// FluentValidation, and enforced at startup.
        /// </summary>
        /// <param name="configurationSection">
        /// The configuration section name to bind <typeparamref name="TOptions"/> to.
        /// </param>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> instance to bind from. Pass <see langword="null"/> to bind from the
        /// configuration resolved out of the service provider at validation time.
        /// </param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> instance for chained configuration.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="configurationSection"/> is null, empty, or whitespace.
        /// </exception>
        public OptionsBuilder<TOptions> AddValidatedOptions<TOptions, TValidator>(
            string configurationSection,
            IConfiguration? configuration = null
        )
            where TOptions : class
            where TValidator : class, IValidator<TOptions>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationSection);

            services.AddSingleton<IValidator<TOptions>, TValidator>();

            var builder = services.AddOptions<TOptions>();
            builder = configuration is null
                ? builder.BindConfiguration(configurationSection)
                : builder.Bind(configuration.GetSection(configurationSection));

            return builder
                .AddConfigurationValidation()
                .ValidateOnStart();
        }
    }
}
