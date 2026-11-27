using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Testing.Resetting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// Starts the Insights services with a fake clock and the doubles of the chat models. It wraps the default fixture
/// and resets it before each test.
/// </summary>
public sealed class ChatTurnFixture : IAsyncLifetime, IResetBeforeEachTest
{
    public ChatTurnFixture()
    {
        Time = new FakeTimeProvider(ChatTestData.Now);

        Inner = new InsightsFixture(
            configureSettings: null,
            services =>
            {
                services.AddSingleton<TimeProvider>(Time);
                Fakes.Register(services);
            });
    }

    public FakeTimeProvider Time { get; }

    public ChatTurnFakes Fakes { get; } = new();

    public InsightsFixture Inner { get; }

    public ValueTask InitializeAsync() => Inner.InitializeAsync();

    public async ValueTask ResetAsync()
    {
        Fakes.Reset();
        await Inner.ResetAsync();
    }

    public ValueTask DisposeAsync() => Inner.DisposeAsync();
}
