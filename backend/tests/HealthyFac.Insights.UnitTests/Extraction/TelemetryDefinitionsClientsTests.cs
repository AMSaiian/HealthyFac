using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Extraction;

[Feature(TestFeatures.Ingest)]
public sealed class TelemetryDefinitionsClientsTests
{
    [Theory]
    [MemberData(
        nameof(TelemetryDefinitionsClientsTestData.Accessors),
        MemberType = typeof(TelemetryDefinitionsClientsTestData))]
    public void Create_ByTheTokenOfTheCall_CreatesTheUserClientOrTheServiceClient(
        string caseName,
        IIncomingAccessTokenAccessor? accessor,
        string expected
    )
    {
        var factory = new RecordingGrpcClientFactory();

        new TelemetryDefinitionsClients(factory, accessor).Create();

        factory.RequestedNames.Should().Equal([expected], caseName);
    }
}
