using HealthyFac.Shared.IntegrationTests.Messaging.Data;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

[Collection(MessagingTransportCollection.Name)]
public sealed class BrokerConnectionProviderTests(TransportCollectionFixture fixture)
{
    private readonly TransportCollectionFixture _fixture = fixture;

    [Fact]
    public async Task GetConnectionAsync_CalledTwice_ReturnsSameConnection()
    {
        await using var provider = _fixture.CreateBrokerConnectionProvider();
        var firstConnection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);

        var secondConnection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);

        secondConnection.Should().BeSameAs(firstConnection);
    }

    [Fact]
    public async Task GetConnectionAsync_AfterConnectionClosed_ReturnsNewOpenConnection()
    {
        await using var provider = _fixture.CreateBrokerConnectionProvider();
        var firstConnection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);
        await firstConnection.CloseAsync(
            BrokerTestData.CloseReplyCode,
            BrokerTestData.CloseReplyText,
            BrokerTestData.CloseTimeout,
            abort: false,
            TestContext.Current.CancellationToken);

        var secondConnection = await provider.GetConnectionAsync(TestContext.Current.CancellationToken);

        secondConnection.Should().NotBeSameAs(firstConnection);
        secondConnection.IsOpen.Should().BeTrue();
    }
}
