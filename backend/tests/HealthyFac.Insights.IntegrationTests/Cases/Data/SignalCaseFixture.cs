using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Testing.Resetting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.IntegrationTests.Cases.Data;

/// <summary>
/// Starts the Insights services with a fake clock, so a test reads the due time of a queued run exactly, and with the
/// doubles of the agent models. It wraps the default fixture and resets it before each test.
/// </summary>
public sealed class SignalCaseFixture : IAsyncLifetime, IResetBeforeEachTest
{
    public SignalCaseFixture()
    {
        Time = new FakeTimeProvider(CaseTestData.Now);
        Inner = new InsightsFixture(
            configureSettings: null,
            services =>
            {
                services.AddSingleton<TimeProvider>(Time);
                Agents.Register(services);
            });
    }

    public FakeTimeProvider Time { get; }

    public AgentFakes Agents { get; } = new();

    public InsightsFixture Inner { get; }

    public ValueTask InitializeAsync() => Inner.InitializeAsync();

    public async ValueTask ResetAsync()
    {
        Agents.Reset();
        await Inner.ResetAsync();
    }

    public ValueTask DisposeAsync() => Inner.DisposeAsync();
}
