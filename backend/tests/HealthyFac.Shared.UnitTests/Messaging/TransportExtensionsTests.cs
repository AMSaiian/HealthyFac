using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.UnitTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class TransportExtensionsTests
{
    [Fact]
    public void AddIntegrationMessage_WithoutHandler_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddIntegrationMessage<RegistryFirstMessage>();

        act.Should().Throw<InvalidOperationException>().WithMessage(RegistryTestData.MessageHandlerMissingText);
    }

    [Fact]
    public void AddIntegrationMessage_WithTwoMessages_RegistersBothInOneRegistry()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<RegistryFirstMessage, Unit>, RegistryMessageHandler<RegistryFirstMessage>>();
        services
            .AddScoped<IRequestHandler<RegistrySecondMessage, Unit>, RegistryMessageHandler<RegistrySecondMessage>>();

        services.AddIntegrationMessage<RegistryFirstMessage>().AddIntegrationMessage<RegistrySecondMessage>();

        var registries = services
            .Where(descriptor => descriptor.ServiceType == typeof(IntegrationMessageRegistry))
            .Select(descriptor => descriptor.ImplementationInstance)
            .ToList();

        registries.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<IntegrationMessageRegistry>()
            .Which.MessageTypes.Should()
            .BeEquivalentTo(
                RegistryTestData.FirstMessageType,
                RegistryTestData.SecondMessageType);
    }
}
