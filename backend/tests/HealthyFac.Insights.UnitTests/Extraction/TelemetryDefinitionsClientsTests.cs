using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class TelemetryDefinitionsClientsTests
{
    [Theory]
    [InlineData(true, TelemetryClientNames.User.Definitions)]
    [InlineData(false, TelemetryClientNames.Definitions)]
    public void NameFor_ByTheTokenOfTheCall_PicksTheUserClientOrTheServiceClient(bool hasUserToken, string expected)
    {
        TelemetryDefinitionsClients.NameFor(hasUserToken).Should().Be(expected);
    }

    [Fact]
    public void Create_WithARequestThatHoldsAUserToken_CreatesTheUserClient()
    {
        var factory = new RecordingGrpcClientFactory();

        new TelemetryDefinitionsClients(factory, new UserTokenAccessor()).Create();

        factory.RequestedNames.Should().Equal(TelemetryClientNames.User.Definitions);
    }

    [Fact]
    public void Create_WithNoUserToken_CreatesTheServiceClient()
    {
        var factory = new RecordingGrpcClientFactory();

        new TelemetryDefinitionsClients(factory, new NoUserTokenAccessor()).Create();

        factory.RequestedNames.Should().Equal(TelemetryClientNames.Definitions);
    }

    [Fact]
    public void Create_WithNoAccessor_CreatesTheServiceClient()
    {
        var factory = new RecordingGrpcClientFactory();

        new TelemetryDefinitionsClients(factory).Create();

        factory.RequestedNames.Should().Equal(TelemetryClientNames.Definitions);
    }
}
