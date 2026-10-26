using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Shared.UnitTests.Messaging.Data;

namespace HealthyFac.Shared.UnitTests.Messaging;

public sealed class IntegrationMessageRegistryTests
{
    [Fact]
    public void Register_SameMessageTwice_ThrowsHandlerAlreadyRegistered()
    {
        var registry = new IntegrationMessageRegistry();
        registry.Register<RegistryFirstMessage>();

        var act = () => registry.Register<RegistryFirstMessage>();

        act.Should().Throw<InvalidOperationException>().WithMessage(RegistryTestData.HandlerAlreadyRegisteredText);
    }

    [Fact]
    public void Register_SameNameWithOtherType_Throws()
    {
        var registry = new IntegrationMessageRegistry();
        registry.Register<RegistryFirstMessage>();

        var act = () => registry.Register<RegistrySameNameMessage>();

        act.Should().Throw<InvalidOperationException>().WithMessage(RegistryTestData.MessageTypeConflictText);
    }

    [Fact]
    public void TryGetDispatch_WithUnknownName_ReturnsFalse()
    {
        var registry = new IntegrationMessageRegistry();
        registry.Register<RegistryFirstMessage>();

        var isFound = registry.TryGetDispatch(RegistryTestData.UnknownMessageType, out _);

        isFound.Should().BeFalse();
    }
}
