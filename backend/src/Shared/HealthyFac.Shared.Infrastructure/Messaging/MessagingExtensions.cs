using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.Infrastructure.Messaging;

public static class MessagingExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the database context of a service together with the outbox and the inbox. Every context
        /// interface resolves the same scoped <typeparamref name="TContext"/>, so the business changes of a use
        /// case and its outbox rows commit in one <c>SaveChangesAsync</c> call, and inbox inserts use the same
        /// connection and transaction. Register a service context only through
        /// this method, because a separate registration creates a second instance and splits the transaction.
        /// </summary>
        /// <typeparam name="TInterface">The Application interface that use cases inject.</typeparam>
        /// <typeparam name="TContext">The concrete context of the service.</typeparam>
        /// <param name="configureOptions">Configures the provider, the interceptors, and the logging.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddMessagingDbContext<TInterface, TContext>(
            Action<IServiceProvider, DbContextOptionsBuilder> configureOptions
        )
            where TInterface : class
            where TContext : DbContext, TInterface, IOutboxDbContext, IInboxDbContext
        {
            services.AddDbContext<TContext>(configureOptions);
            services.AddScoped<TInterface>(provider => provider.GetRequiredService<TContext>());
            services.AddScoped<IOutboxDbContext>(provider => provider.GetRequiredService<TContext>());
            services.AddScoped<IInboxDbContext>(provider => provider.GetRequiredService<TContext>());
            services.AddScoped<IOutbox, Outbox>();
            services.AddScoped<IInbox, Inbox>();

            return services;
        }
    }

    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        /// Maps the outbox table. The pending-message index filter uses snake case column names, so call
        /// <c>ToSnakeCaseConventions</c> after this method.
        /// </summary>
        /// <returns>The <see cref="ModelBuilder"/> instance for chained configuration.</returns>
        public ModelBuilder ApplyOutboxConfiguration()
            => modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        /// <summary>
        /// Maps the inbox table. The pending-message index filter uses snake case column names, so call
        /// <c>ToSnakeCaseConventions</c> after this method.
        /// </summary>
        /// <returns>The <see cref="ModelBuilder"/> instance for chained configuration.</returns>
        public ModelBuilder ApplyInboxConfiguration()
            => modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
