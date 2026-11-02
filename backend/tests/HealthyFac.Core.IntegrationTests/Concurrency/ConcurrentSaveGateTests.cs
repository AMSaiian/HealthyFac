using System.Collections.Concurrent;
using HealthyFac.Core.IntegrationTests.Concurrency.Data;
using HealthyFac.Testing.Persistence;

namespace HealthyFac.Core.IntegrationTests.Concurrency;

public sealed class ConcurrentSaveGateTests(GateDatabaseFixture fixture) : IClassFixture<GateDatabaseFixture>
{
    private readonly GateDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Race_WithTwoParticipants_ReleasesBothFirstSavesTogether()
    {
        var gate = new ConcurrentSaveGate();
        var events = new ConcurrentQueue<string>();
        using var race = gate.StartRace(ConcurrentSaveGateTestData.ParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(async () =>
            {
                await _fixture.SaveRecordAsync(gate);
                events.Enqueue(ConcurrentSaveGateTestData.FastParticipantSaved);
            }),
            race.RunAsync(async () =>
            {
                await Task.Delay(ConcurrentSaveGateTestData.SlowParticipantDelay);
                events.Enqueue(ConcurrentSaveGateTestData.SlowParticipantReachedSave);
                await _fixture.SaveRecordAsync(gate);
            }));

        failures.Should().OnlyContain(failure => failure == null);
        events.Should()
            .ContainInOrder(
                ConcurrentSaveGateTestData.SlowParticipantReachedSave,
                ConcurrentSaveGateTestData.FastParticipantSaved);
        race.SaveCounts.Should().Equal(1, 1);
    }

    [Fact]
    public async Task Race_WithSecondSaveOfOneParticipant_DoesNotCountItTwice()
    {
        var gate = new ConcurrentSaveGate();
        using var race = gate.StartRace(ConcurrentSaveGateTestData.ParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(async () =>
            {
                await _fixture.SaveRecordAsync(gate);
                await _fixture.SaveRecordAsync(gate);
            }),
            race.RunAsync(() => _fixture.SaveRecordAsync(gate)));

        failures.Should().OnlyContain(failure => failure == null);
        race.SaveCounts.Should().Equal(2, 1);
    }

    [Fact]
    public async Task Race_WhenOneParticipantNeverSaves_ThrowsTimeoutException()
    {
        var gate = new ConcurrentSaveGate(ConcurrentSaveGateTestData.ShortTimeout);
        using var race = gate.StartRace(ConcurrentSaveGateTestData.ParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.SaveRecordAsync(gate)),
            race.RunAsync(() => Task.CompletedTask));

        failures[0]
            .Should()
            .BeOfType<TimeoutException>()
            .Which.Message.Should()
            .Contain(ConcurrentSaveGateTestData.OneOfTwoArrived);
        failures[1].Should().BeNull();
    }

    [Fact]
    public async Task Save_OutsideRace_PassesAtOnce()
    {
        var gate = new ConcurrentSaveGate();
        using var race = gate.StartRace(ConcurrentSaveGateTestData.ParticipantCount);

        var act = () => _fixture.SaveRecordAsync(gate);

        await act.Should().CompleteWithinAsync(ConcurrentSaveGateTestData.OutsideSaveLimit);
    }
}
