using HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;
using HealthyFac.Telemetry.UnitTests.Intake.Data;

namespace HealthyFac.Telemetry.UnitTests.Intake;

public sealed class IntakeFailuresTests
{
    [Theory]
    [MemberData(nameof(IntakeFailureTestData.TransientFailures), MemberType = typeof(IntakeFailureTestData))]
    public void IsTransient_WithTransientFailure_ReturnsTrue(Exception exception)
    {
        var isTransient = IntakeFailures.IsTransient(exception);

        isTransient.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(IntakeFailureTestData.PermanentFailures), MemberType = typeof(IntakeFailureTestData))]
    public void IsTransient_WithPermanentFailure_ReturnsFalse(Exception exception)
    {
        var isTransient = IntakeFailures.IsTransient(exception);

        isTransient.Should().BeFalse();
    }
}
