using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.Decorators;

namespace HealthyFac.Core.Application;

/// <summary>
/// Registers application layer services into the dependency injection container.
/// </summary>
public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers application request handlers, decorators, and fluent validators.
        /// </summary>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddApplicationServices()
        {
            var applicationAssembly = typeof(MachineErrors).Assembly;

            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly)
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Decorate(typeof(IRequestHandler<,>), typeof(ValidationRequestHandlerDecorator<,>));
            services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingRequestHandlerDecorator<,>));

            services.AddValidatorsFromAssembly(applicationAssembly);

            return services;
        }
    }
}
