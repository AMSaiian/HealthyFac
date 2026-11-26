using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;
using HealthyFac.MachineSimulator.Machines;
using HealthyFac.MachineSimulator.Simulation;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation;

[Collection(SimulatorCollection.Name)]
[Feature(TestFeatures.Simulation)]
public sealed class SimulatorInstanceLockTests(SimulatorFixture fixture)
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task StartAsync_WhileTheSimulatorRuns_LogsErrorAndThrows()
    {
        var logger = new CapturingLogger<SimulatorInstanceLock>();
        await using var instanceLock = CreateLock(_fixture.SimulatorCoreConnectionString, logger);

        var act = () => instanceLock.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(SimulatorErrors.AlreadyRunning);
        logger.Entries.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task SecondSimulator_OnTheSameDatabase_FailsToStart()
    {
        await using var second = new SimulatorFactory(
            _fixture.SimulatorSettings,
            _fixture.Clock,
            new CapturingLoggerProvider());

        var act = () => Task.FromResult(second.CreateClient());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(SimulatorErrors.AlreadyRunning);
    }

    [Fact]
    public async Task StartAsync_OnADatabaseWithoutASimulator_HoldsTheLockUntilStop()
    {
        await using var instanceLock = CreateLock(
            _fixture.SimulatorTelemetryConnectionString,
            new CapturingLogger<SimulatorInstanceLock>());

        await instanceLock.StartAsync(CancellationToken.None);

        var whileHeld = await TryTakeLockAsync();
        await instanceLock.StopAsync(CancellationToken.None);
        var afterStop = await TryTakeLockAsync();

        whileHeld.Should().BeFalse();
        afterStop.Should().BeTrue();
    }

    private static SimulatorInstanceLock CreateLock(string connectionString, ILogger<SimulatorInstanceLock> logger) =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{SimulatorDatabaseConstants.ConnectionStrings.Core}"] = connectionString,
                    })
                .Build(),
            logger);

    private async Task<bool> TryTakeLockAsync()
    {
        await using var connection = new NpgsqlConnection(
            new NpgsqlConnectionStringBuilder(_fixture.SimulatorTelemetryConnectionString) { Pooling = false }
                .ConnectionString);

        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(cmdText: "SELECT pg_try_advisory_lock(@key)", connection);
        command.Parameters.AddWithValue(parameterName: "key", SimulatorInstanceLock.LockKey);
        var isTaken = (bool)(await command.ExecuteScalarAsync())!;

        if (isTaken)
        {
            await using var unlock = new NpgsqlCommand(cmdText: "SELECT pg_advisory_unlock(@key)", connection);
            unlock.Parameters.AddWithValue(parameterName: "key", SimulatorInstanceLock.LockKey);
            await unlock.ExecuteScalarAsync();
        }

        return isTaken;
    }
}
