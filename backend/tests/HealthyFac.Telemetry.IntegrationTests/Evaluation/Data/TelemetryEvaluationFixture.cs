using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Testing.Resetting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

/// <summary>
/// Starts PostgreSQL and the Telemetry services with a fake clock and the evaluator worker turned off, so a test runs
/// the evaluation of a machine by hand and moves the time itself.
/// </summary>
public sealed class TelemetryEvaluationFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private TelemetryDatabaseFixture? _database;

    public FakeTimeProvider Time { get; } = new(EvaluationTestData.Start);

    public FailRunSaveInterceptor RunFailure { get; } = new();

    /// <summary>
    /// The time at the start of the running test. The fake clock only moves forward, so each test starts where the
    /// last one stopped.
    /// </summary>
    public DateTime Origin { get; private set; } = EvaluationTestData.Start;

    public TelemetryDatabaseFixture Database => _database!;

    public DateTime At(double seconds) => Origin.AddSeconds(seconds);

    public async ValueTask InitializeAsync()
    {
        _database = new TelemetryDatabaseFixture(
            settings => settings["Telemetry:Detection:IsWorkerEnabled"] = bool.FalseString,
            services =>
            {
                services.AddSingleton<TimeProvider>(Time);
                services.ConfigureDbContext<TelemetryDbContext>(options => options.AddInterceptors(RunFailure));
            });

        await _database.InitializeAsync();
    }

    public async ValueTask ResetAsync()
    {
        await Database.ResetAsync();
        Origin = Time.GetUtcNow().UtcDateTime;
        RunFailure.Reset();
        Database.GetSingleton<FleetSilence>().Reset();
    }

    public async ValueTask DisposeAsync() => await _database!.DisposeAsync();

    public IServiceScope CreateActorScope(ActorContext actor) => Database.CreateActorScope(actor);
}
