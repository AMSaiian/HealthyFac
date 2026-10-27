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
}
