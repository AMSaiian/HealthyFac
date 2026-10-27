using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Email;

/// <summary>
/// Provides extension methods for registering an email provider.
/// </summary>
public static class EmailExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated SMTP options and <see cref="SmtpEmailSender"/> as the
        /// <see cref="IEmailSender"/>. This method is the choice of provider for a deployment: another provider adds
        /// its own <c>Add...Email</c> method, and the host calls one of them.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddSmtpEmail(IConfiguration configuration)
        {
            services.AddValidatedOptions<SmtpOptions, SmtpOptionsValidator>(SmtpOptions.SectionName, configuration);
            services.AddTransient<IEmailSender, SmtpEmailSender>();

            return services;
        }
    }
}
