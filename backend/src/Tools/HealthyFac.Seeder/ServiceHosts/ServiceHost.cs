using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// The service provider of one service, built from its own registrations. The seed job calls its use cases through
/// the decorated handlers, so the validators and the pre-handler checks run as in the service. The host never starts
/// a hosted service: no message goes through the broker, and no processor runs by itself.
/// </summary>
public sealed class ServiceHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private InboxProcessor? _inboxProcessor;
    private readonly Func<IServiceProvider, CancellationToken, Task> _initialize;

    internal ServiceHost(
        ServiceName name,
        ServiceProvider provider,
        IReadOnlyList<ServiceDescriptor> registrations,
        Func<IServiceProvider, CancellationToken, Task> initialize
    )
    {
        Name = name;
        _provider = provider;
        Registrations = registrations;
        _initialize = initialize;
    }

    public ServiceName Name { get; }

    /// <summary>The registrations of the host. Tests check which types the host registers.</summary>
    internal IReadOnlyList<ServiceDescriptor> Registrations { get; }

    /// <summary>Resolves a singleton of the host. Tests reach the token parts and the clock through it.</summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>The service.</returns>
    internal TService GetService<TService>()
        where TService : notnull => _provider.GetRequiredService<TService>();

    /// <summary>The message types that the service handles.</summary>
    internal IntegrationMessageRegistry MessageRegistry => _provider.GetRequiredService<IntegrationMessageRegistry>();

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();

    /// <summary>
    /// Applies the migrations and seeds the required data of the service, as the service does at its start.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the database is ready.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken) => _initialize(_provider, cancellationToken);

    /// <summary>Runs a request with a response through the decorated handler, in a new scope of the actor.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="actor">The actor of the scope.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The response of the handler.</returns>
    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        ActorContext actor,
        TRequest request,
        CancellationToken cancellationToken
    )
        where TRequest : IRequest<TResponse>
    {
        using var scope = CreateScope(actor);

        return await scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
            .HandleAsync(request, cancellationToken);
    }

    /// <summary>Runs a request without a response through the decorated handler, in a new scope of the actor.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="actor">The actor of the scope.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    public async Task SendAsync<TRequest>(ActorContext actor, TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        using var scope = CreateScope(actor);

        await scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest>>()
            .HandleAsync(request, cancellationToken);
    }

    /// <summary>Opens a scope that runs as the actor, for a call into the domain or a read of the database.</summary>
    /// <param name="actor">The actor of the scope.</param>
    /// <returns>The scope. The caller disposes it.</returns>
    public IServiceScope CreateScope(ActorContext actor) =>
        _provider.GetRequiredService<IServiceScopeFactory>().CreateActorScope(actor);

    /// <summary>
    /// Runs every validator of the service for a request, as the validation decorator does. It reads no database, so
    /// the seed check can run before the first write.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>One text for each failure. The list is empty when the request is valid or has no validator.</returns>
    public async Task<IReadOnlyList<string>> ValidateAsync(object request, CancellationToken cancellationToken)
    {
        using var scope = CreateScope(ServiceActor.DatabaseInitialization.For(Name));
        var validatorType = typeof(IValidator<>).MakeGenericType(request.GetType());
        var failures = new List<string>();

        foreach (var validator in scope.ServiceProvider.GetServices(validatorType).OfType<IValidator>())
        {
            var result = await validator.ValidateAsync(new ValidationContext<object>(request), cancellationToken);
            failures.AddRange(result.Errors.Select(failure => $"{failure.PropertyName}: {failure.ErrorMessage}"));
        }

        return failures;
    }

    /// <summary>
    /// Runs the inbox processor of the service until no inbox row is due. The processor class is the one that the
    /// service registers as a hosted service, so a message runs through its real use case, transaction, and retry
    /// rules. The host creates it directly: resolving the hosted services would also build the broker client.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A task that completes when the run ends.</returns>
    public async Task DrainInboxAsync(CancellationToken cancellationToken)
    {
        var processor = _inboxProcessor ??= ActivatorUtilities.CreateInstance<InboxProcessor>(_provider);

        await processor.RunAsync(cancellationToken);
    }
}
