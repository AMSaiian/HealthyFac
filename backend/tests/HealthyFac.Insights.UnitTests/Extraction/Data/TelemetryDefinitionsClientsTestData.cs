using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;
using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class TelemetryDefinitionsClientsTestData
{
    /// <summary>The case name, the accessor of the host (<see langword="null"/> for none), and the client.</summary>
    public static TheoryData<string, IIncomingAccessTokenAccessor?, string> Accessors => new()
    {
        { "UserToken", new UserTokenAccessor(), TelemetryClientNames.User.Definitions },
        { "NoUserToken", new NoUserTokenAccessor(), TelemetryClientNames.Definitions },
        { "NoAccessor", null, TelemetryClientNames.Definitions },
    };
}
