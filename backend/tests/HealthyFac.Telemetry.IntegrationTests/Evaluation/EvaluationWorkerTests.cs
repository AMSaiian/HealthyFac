using HealthyFac.Telemetry.Infrastructure.Evaluation;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class EvaluationWorkerTests(TelemetryEvaluationFixture fixture)
{
    private const int MachineCount = 5;
    private const int EvaluationIntervalSeconds = 10;
    private const int LeaseSeconds = 60;

    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task RunOnceAsync_WithTwoWorkersOverFiveDueMachines_RunsEachMachineOnce()
    {
        var machineIds = await RegisterMachinesAsync();
        var worker = _fixture.Database.GetHostedService<EvaluationWorker>();
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));

        var counts = await Task.WhenAll(
            worker.RunOnceAsync(CancellationToken.None),
            worker.RunOnceAsync(CancellationToken.None));

        var again = await worker.RunOnceAsync(CancellationToken.None);

        var evaluations = new List<DateTime?>();
        var dueTimes = new List<DateTime>();

        foreach (var machineId in machineIds)
        {
            var evaluation = await _fixture.LoadEvaluationAsync(machineId);
            evaluations.Add(evaluation.LastRunAt);
            dueTimes.Add(evaluation.DueAt);
        }

        counts.Sum().Should().Be(MachineCount);
        again.Should().Be(expected: 0);
        evaluations.Should().OnlyContain(lastRunAt => lastRunAt == _fixture.At(1));
        dueTimes.Should().OnlyContain(dueAt => dueAt == _fixture.At(1 + EvaluationIntervalSeconds));
    }

    [Fact]
    public async Task ClaimNextAsync_WithFiveParallelCallsOverFiveDueMachines_ClaimsEachMachineOnce()
    {
        var machineIds = await RegisterMachinesAsync();
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));

        var claims = await Task.WhenAll(Enumerable.Range(start: 0, MachineCount).Select(_ => ClaimAsync()));
        var extra = await ClaimAsync();

        claims.Select(claimed => claimed!.MachineId).Should().BeEquivalentTo(machineIds);
        extra.Should().BeNull();
    }

    [Fact]
    public async Task ClaimNextAsync_WithAnExpiredLease_TakesTheMachinesOver()
    {
        await RegisterMachinesAsync();
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));
        var firstRound = await DrainClaimsAsync();

        var whileLeased = await ClaimAsync();
        _fixture.Time.SetUtcNow(_fixture.At(1 + LeaseSeconds + 1));
        var secondRound = await DrainClaimsAsync();

        firstRound.Should().Be(MachineCount);
        whileLeased.Should().BeNull();
        secondRound.Should().Be(MachineCount);
    }

    [Fact]
    public async Task ClaimNextAsync_AfterARun_ClaimsTheMachineOnlyWhenItIsDueAgain()
    {
        await RegisterMachinesAsync();
        var worker = _fixture.Database.GetHostedService<EvaluationWorker>();
        _fixture.Time.SetUtcNow(_fixture.At(seconds: 1));
        await worker.RunOnceAsync(CancellationToken.None);

        _fixture.Time.SetUtcNow(_fixture.At(1 + EvaluationIntervalSeconds - 1));
        var beforeDue = await ClaimAsync();
        _fixture.Time.SetUtcNow(_fixture.At(1 + EvaluationIntervalSeconds));
        var atDue = await ClaimAsync();

        beforeDue.Should().BeNull();
        atDue.Should().NotBeNull();
    }

    private async Task<List<Guid>> RegisterMachinesAsync()
    {
        var machineIds = new List<Guid>();

        for (var index = 0; index < MachineCount; index++)
        {
            machineIds.Add(await _fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId));
        }

        return machineIds;
    }

    private async Task<ClaimedEvaluation?> ClaimAsync()
    {
        using var scope = _fixture.Database.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<EvaluationClaims>()
            .ClaimNextAsync(CancellationToken.None);
    }

    private async Task<int> DrainClaimsAsync()
    {
        var count = 0;

        while (await ClaimAsync() is not null)
        {
            count++;
        }

        return count;
    }
}
