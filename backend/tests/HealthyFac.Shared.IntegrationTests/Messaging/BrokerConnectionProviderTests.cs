using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class BrokerConnectionProviderTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    [Fact]
    public async Task GetConnectionAsync_CalledTwice_ReturnsSameConnection()
    {
        await using var provider = fixture.CreateBrokerConnectionProvider();
        var firstConnection = await provider.GetConnectionAsync();

        var secondConnection = await provider.GetConnectionAsync();

        secondConnection.Should().BeSameAs(firstConnection);
    }

    [Fact]
    public async Task GetConnectionAsync_AfterConnectionClosed_ReturnsNewOpenConnection()
    {
        await using var provider = fixture.CreateBrokerConnectionProvider();
        var firstConnection = await provider.GetConnectionAsync();
        await firstConnection.CloseAsync(
            BrokerTestData.CloseReplyCode,
            BrokerTestData.CloseReplyText,
            BrokerTestData.CloseTimeout,
            false);

        var secondConnection = await provider.GetConnectionAsync();

        secondConnection.Should().NotBeSameAs(firstConnection);
        secondConnection.IsOpen.Should().BeTrue();
    }
}
