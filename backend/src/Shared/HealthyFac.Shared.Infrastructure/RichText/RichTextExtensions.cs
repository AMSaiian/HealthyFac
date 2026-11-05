using HealthyFac.Shared.Application.RichText;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Infrastructure.RichText;

/// <summary>
/// Provides extension methods for registering the rich text policy.
/// </summary>
public static class RichTextExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="HtmlSanitizerRichTextPolicy"/> as the rich text policy. The policy keeps no state
        /// between calls, so one instance serves all requests.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSharedRichText()
        {
            services.TryAddSingleton<IRichTextPolicy, HtmlSanitizerRichTextPolicy>();

            return services;
        }
    }
}
