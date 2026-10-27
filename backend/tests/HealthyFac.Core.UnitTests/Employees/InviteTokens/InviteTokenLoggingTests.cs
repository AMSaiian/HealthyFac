using HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Web.Logging;
using Serilog;
using Serilog.Events;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens;

public sealed class InviteTokenLoggingTests
{
    [Theory]
    [MemberData(
        nameof(InviteTokenLoggingTestData.RequestsWithToken),
        MemberType = typeof(InviteTokenLoggingTestData)
    )]
    public void Destructure_WithInviteRequest_MasksToken(object request)
    {
        var sink = new CapturingLogEventSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Debug("Use case received request {@Request}", request);

        var logged = sink.Events.Should().ContainSingle().Subject;
        var structure = logged.Properties["Request"].Should().BeOfType<StructureValue>().Subject;
        var token = structure.Properties.Single(property =>
            property.Name == InviteTokenLoggingTestData.TokenPropertyName);

        token.Value.Should().BeOfType<ScalarValue>().Which.Value.Should().Be(MaskingConstants.DefaultMask);
    }
}
