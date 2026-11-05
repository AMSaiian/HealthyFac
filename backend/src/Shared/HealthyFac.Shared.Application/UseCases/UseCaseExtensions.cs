using System.Reflection;
using FluentValidation;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.Decorators;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Shared.Application.UseCases;

/// <summary>
/// Provides the registration of use cases. It is the only place that registers them, for every service.
/// </summary>
public static class UseCaseExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers every <see cref="IRequestHandler{TRequest,TResponse}"/>, every
        /// <see cref="IPreHandlerCheck{TRequest}"/>, and every validator of the assembly, wraps the handlers with the
        /// downstream scope decorator, then the pre-handler check decorator, then the validation decorator, and then
        /// the logging decorator (the run order is the reverse), and registers the adapter for the use cases without
        /// a result. It also sets the JSON names of the validation error keys (<see cref="ValidationNames"/>).
        /// </summary>
        /// <param name="applicationAssembly">The assembly of the service application layer.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when one request has more than one handler. This check also keeps the rule that a message type
        /// has one handler.
        /// Also thrown when a handled request has no pre-handler check or more than one (see
        /// <see cref="UseCaseErrors.PreHandlerCheckMissing"/> and
        /// <see cref="UseCaseErrors.PreHandlerCheckAlreadyRegistered"/>).
        /// Also thrown when <c>AddUseCases</c> already ran for the collection (see
        /// <see cref="UseCaseErrors.UseCasesAlreadyAdded"/>).
        /// </exception>
        public IServiceCollection AddUseCases(Assembly applicationAssembly) =>
            services.AddUseCases(applicationAssembly.GetTypes());

        /// <summary>
        /// Registers the handlers among the given types and the validators of the assemblies of those types. The
        /// assembly overload calls it with all types of the assembly. A test calls it with a short list of types.
        /// </summary>
        /// <param name="candidateTypes">The types to scan for handlers and validators.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when one request has more than one handler.
        /// Also thrown when a handled request has no pre-handler check or more than one (see
        /// <see cref="UseCaseErrors.PreHandlerCheckMissing"/> and
        /// <see cref="UseCaseErrors.PreHandlerCheckAlreadyRegistered"/>).
        /// Also thrown when <c>AddUseCases</c> already ran for the collection (see
        /// <see cref="UseCaseErrors.UseCasesAlreadyAdded"/>).
        /// </exception>
        internal IServiceCollection AddUseCases(IReadOnlyCollection<Type> candidateTypes)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(IRequestHandler<>)))
            {
                throw UseCaseErrors.UseCasesAlreadyAdded();
            }

            var handlerTypes = candidateTypes.Where(IsHandlerImplementation).ToList();

            services.Scan(scan => scan
                .FromTypes(handlerTypes)
                .AddClasses()
                .AsImplementedInterfaces(IsRequestHandlerInterface)
                .WithScopedLifetime());

            services.EnsureOneHandlerForEachRequest();

            var preHandlerCheckTypes = candidateTypes.Where(IsPreHandlerCheckImplementation).ToList();

            services.Scan(scan => scan
                .FromTypes(preHandlerCheckTypes)
                .AddClasses()
                .AsImplementedInterfaces(IsPreHandlerCheckInterface)
                .WithScopedLifetime());

            services.EnsureOnePreHandlerCheckForEachRequest();

            services.TryAddSingleton<IDownstreamScopeHolder, DownstreamScopeHolder>();
            services.Decorate(typeof(IRequestHandler<,>), typeof(DownstreamScopeRequestHandlerDecorator<,>));
            services.Decorate(typeof(IRequestHandler<,>), typeof(PreHandlerCheckRequestHandlerDecorator<,>));
            services.Decorate(typeof(IRequestHandler<,>), typeof(ValidationRequestHandlerDecorator<,>));
            services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingRequestHandlerDecorator<,>));
            services.AddScoped(typeof(IRequestHandler<>), typeof(UnitRequestHandlerAdapter<>));
            services.AddValidatorsFromAssemblies(candidateTypes.Select(type => type.Assembly).Distinct());
            ValidationNames.UseJsonNames();

            return services;
        }

        private void EnsureOnePreHandlerCheckForEachRequest()
        {
            var preHandlerChecksByRequest = services
                .Where(descriptor => IsPreHandlerCheckInterface(descriptor.ServiceType))
                .GroupBy(descriptor => descriptor.ServiceType.GetGenericArguments()[0])
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (var handlerDescriptor in services.Where(descriptor =>
                IsRequestHandlerInterface(descriptor.ServiceType)))
            {
                var requestType = handlerDescriptor.ServiceType.GetGenericArguments()[0];

                if (!preHandlerChecksByRequest.TryGetValue(requestType, out var count))
                {
                    throw UseCaseErrors.PreHandlerCheckMissing(requestType);
                }

                if (count > 1)
                {
                    throw UseCaseErrors.PreHandlerCheckAlreadyRegistered(requestType);
                }
            }
        }

        private void EnsureOneHandlerForEachRequest()
        {
            var duplicated = services
                .Where(descriptor => IsRequestHandlerInterface(descriptor.ServiceType))
                .GroupBy(descriptor => descriptor.ServiceType)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicated is not null)
            {
                throw UseCaseErrors.HandlerAlreadyRegistered(duplicated.Key.GetGenericArguments()[0]);
            }
        }
    }

    private static bool IsHandlerImplementation(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
        && type.GetInterfaces().Any(IsRequestHandlerInterface);

    private static bool IsPreHandlerCheckImplementation(Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
        && type.GetInterfaces().Any(IsPreHandlerCheckInterface);

    private static bool IsPreHandlerCheckInterface(Type type) =>
        type is
        {
            IsGenericType: true,
            ContainsGenericParameters: false,
        }
        && type.GetGenericTypeDefinition() == typeof(IPreHandlerCheck<>);

    private static bool IsRequestHandlerInterface(Type type) =>
        type is
        {
            IsGenericType: true,
            ContainsGenericParameters: false,
        }
        && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
}
