using Microsoft.Extensions.Time.Testing;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Messaging.Data;

public static class MessagingTestServices
{
    public static IServiceScope CreateCompanyScope(this ServiceProvider provider) =>
        provider.GetRequiredService<IServiceScopeFactory>()
            .CreateActorScope(SystemActor.Create(MessagingTestData.CompanyId));

    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(MessagingTestData.Now)));
        services.AddActorContext();
        services.AddMessagingDbContext<ITestServiceDbContext, TestServiceDbContext>((_, options) =>
            options.UseNpgsql(MessagingTestData.ConnectionString));

        return services.BuildServiceProvider(validateScopes: true);
    }
}
