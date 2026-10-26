using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.UnitTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class MessagingExtensionsTests
{
    [Theory]
    [MemberData(nameof(MessagingTestData.ContextInterfaces), MemberType = typeof(MessagingTestData))]
    public void AddMessagingDbContext_WithinOneScope_ResolvesContextInterfaceToSameInstance(Type contextInterface)
    {
        using var provider = MessagingTestServices.Build();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService(contextInterface)
            .Should()
            .BeSameAs(scope.ServiceProvider.GetRequiredService<TestServiceDbContext>());
    }

    [Fact]
    public void AddMessagingDbContext_WhenOutboxAddsMessage_TracksRowOnScopedContext()
    {
        using var provider = MessagingTestServices.Build();
        using var scope = provider.CreateCompanyScope();

        scope.ServiceProvider.GetRequiredService<IOutbox>().Add(new TestPing { Text = MessagingTestData.PingText });

        scope.ServiceProvider.GetRequiredService<TestServiceDbContext>()
            .ChangeTracker.Entries<OutboxMessage>()
            .Should()
            .ContainSingle()
            .Which.Entity.Type.Should()
            .Be(MessagingTestData.PingMessageType);
    }

    [Fact]
    public void AddMessagingDbContext_AcrossScopes_ResolvesDifferentInstances()
    {
        using var provider = MessagingTestServices.Build();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        firstScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>()
            .Should()
            .NotBeSameAs(secondScope.ServiceProvider.GetRequiredService<ITestServiceDbContext>());
    }
}
