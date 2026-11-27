using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class GetCurrentTimeToolTests
{
    private static readonly DateTimeOffset Now = new(
        year: 2026, month: 11, day: 26, hour: 20, minute: 43, second: 0, TimeSpan.Zero);

    private readonly FakeLogger<GetCurrentTimeToolRequestHandler> _logger = new();
    private readonly FakeTimeProvider _time = new(Now);

    [Fact]
    public async Task HandleAsync_WithTheZoneOfTheCompany_ReturnsTheUtcTimeAndTheLocalTime()
    {
        var handler = CreateHandler(new FixedZoneReader(zoneId: "Europe/Kyiv"));

        var response = await handler.HandleAsync(
            new GetCurrentTimeToolRequest(), TestContext.Current.CancellationToken);

        response.UtcNow.Should().Be(Now.UtcDateTime);
        response.TimeZoneId.Should().Be(expected: "Europe/Kyiv");

        response.LocalTime.Should()
            .Be(
                new DateTimeOffset(
                    year: 2026, month: 11, day: 26, hour: 22, minute: 43, second: 0, TimeSpan.FromHours(value: 2)));
    }

    [Fact]
    public async Task HandleAsync_WhenCoreDoesNotAnswer_ReturnsTheUtcTimeWithoutAZoneAndLogsAWarning()
    {
        var handler = CreateHandler(new FixedZoneReader(failure: new ProviderUnavailableException(message: "down")));

        var response = await handler.HandleAsync(
            new GetCurrentTimeToolRequest(), TestContext.Current.CancellationToken);

        response.UtcNow.Should().Be(Now.UtcDateTime);
        response.TimeZoneId.Should().BeNull();
        response.LocalTime.Should().BeNull();
        _logger.Collector.GetSnapshot().Should().ContainSingle(record => record.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task HandleAsync_WithAZoneThatTheHostDoesNotKnow_ReturnsTheUtcTimeWithoutAZone()
    {
        var handler = CreateHandler(new FixedZoneReader(zoneId: "Mars/Olympus"));

        var response = await handler.HandleAsync(
            new GetCurrentTimeToolRequest(), TestContext.Current.CancellationToken);

        response.UtcNow.Should().Be(Now.UtcDateTime);
        response.TimeZoneId.Should().BeNull();
        response.LocalTime.Should().BeNull();
    }

    private GetCurrentTimeToolRequestHandler CreateHandler(ICompanyTimeZoneReader reader) =>
        new(reader, _time, _logger);

    private sealed class FixedZoneReader(string zoneId = "", Exception? failure = null) : ICompanyTimeZoneReader
    {
        public Task<string> ReadTimeZoneIdAsync(CancellationToken cancellationToken) =>
            failure is null ? Task.FromResult(zoneId) : Task.FromException<string>(failure);
    }
}
