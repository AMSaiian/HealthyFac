using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Shared.UnitTests.Observability.Data;

public static class ActivityTraceTestData
{
    public const string SourceName = "HealthyFac.Tests.ActivityTrace";
    public const string ActivityName = "trace";
    public const int Result = 42;

    public static InvalidOperationException CreateFailure() => new(message: "broken");

    public static NotFoundException CreateRejection() => new(message: "missing");
}
