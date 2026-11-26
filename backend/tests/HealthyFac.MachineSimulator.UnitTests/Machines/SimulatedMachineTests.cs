using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.UnitTests.Machines.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.MachineSimulator.UnitTests.Machines;

[Feature(TestFeatures.Simulation)]
public sealed class SimulatedMachineTests
{
    [Theory]
    [MemberData(nameof(SimulatedMachineTestData.GoLiveGates), MemberType = typeof(SimulatedMachineTestData))]
    [Feature(TestFeatures.Machines)]
    public void IsLive_AtLateEveningUtcInKyiv_UsesTheCompanyDay(string caseName, DateOnly goLiveOn, bool expected)
    {
        var machine = SimulatedMachineTestData.Create(SimulatedMachineTestData.Snapshot(goLiveOn: goLiveOn));

        var isLive = machine.IsLive(SimulatedMachineTestData.Now);

        isLive.Should().Be(expected, caseName);
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Tick_WithoutRegistration_MakesNoMessage()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(isRegistered: false);
        var machine = SimulatedMachineTestData.Create(snapshot);

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().BeNull();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Tick_WithRegistration_MakesAMessage()
    {
        var machine = SimulatedMachineTestData.Create(SimulatedMachineTestData.Snapshot());

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().NotBeNull();
    }

    [Fact]
    public void TrySetMode_DrainingWithOwnDuration_ProgressesByTheOwnDuration()
    {
        var machine = SimulatedMachineTestData.Create(
            SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber));

        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Draining, SimulatedMachineTestData.LongDrainDuration);

        for (var second = 0; second < 4 * 3600; second++)
        {
            machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);
        }

        machine.Modules[ModuleName.Spindle].Progress.Should().BeApproximately(expectedValue: 0.5, precision: 1e-6);
    }

    [Fact]
    public void Tick_WithRunOff_MakesNoMessageAndFreezesCountersAndProgress()
    {
        var machine = SimulatedMachineTestData.Create(
            SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber));

        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Draining);
        machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);
        var progress = machine.Modules[ModuleName.Spindle].Progress;
        var hours = machine.Counters["machine.operating_hours"];
        machine.SetRunning(isRunning: false);

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().BeNull();
        machine.Modules[ModuleName.Spindle].Progress.Should().Be(progress);
        machine.Counters["machine.operating_hours"].Should().Be(hours);
    }

    [Fact]
    public void Tick_AfterRunOn_PublishesAgain()
    {
        var machine = SimulatedMachineTestData.Create(SimulatedMachineTestData.Snapshot());
        machine.SetRunning(isRunning: false);
        machine.SetRunning(isRunning: true);

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().NotBeNull();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Tick_GenericMachineInMaintenance_MakesNoMessage()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(
            history: SimulatedMachineTestData.Row(
                MachineState.InMaintenance, SimulatedMachineTestData.Now.AddDays(value: -1)));

        var machine = SimulatedMachineTestData.Create(snapshot);

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().BeNull();
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Tick_DetailedMachineInMaintenance_SendsStoppedValuesAndFreezesTheRunTime()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(
            SimulatedMachineTestData.CncSerialNumber,
            history: SimulatedMachineTestData.Row(
                MachineState.InMaintenance, SimulatedMachineTestData.Now.AddDays(value: -1)));

        var machine = SimulatedMachineTestData.Create(snapshot);
        machine.TrySetMode(ModuleName.Hydraulics, ModuleMode.Draining);

        var result = machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        result.Should().NotBeNull();
        result.Sample.IsRunning.Should().BeFalse();
        result.Sample.Values["spindle.speed"].Should().Be(expected: 0d);
        machine.RunTime.Should().Be(TimeSpan.Zero);
        machine.Modules[ModuleName.Hydraulics].Progress.Should().Be(expected: 0);
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Apply_WithMaintenanceThenOperationalAfterDiscovery_ResetsTheModules()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber);
        var machine = SimulatedMachineTestData.Create(snapshot);
        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Failure);
        var maintenanceStart = SimulatedMachineTestData.Discovery.AddMinutes(value: 10);
        var operationalStart = maintenanceStart.AddSeconds(value: 2);

        var change = machine.Apply(
            SimulatedMachineTestData.WithHistory(
                snapshot,
                SimulatedMachineTestData.Row(
                    MachineState.Operational, SimulatedMachineTestData.Now.AddDays(value: -30)),
                SimulatedMachineTestData.Row(MachineState.InMaintenance, maintenanceStart),
                SimulatedMachineTestData.Row(MachineState.Operational, operationalStart)));

        change.Should().Be(SnapshotChange.Reset);
        machine.Modules[ModuleName.Spindle].Mode.Should().Be(ModuleMode.Expected);
        machine.LastReset.Should().Be(operationalStart);
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Apply_WithOutOfServiceThenOperational_KeepsTheFailure()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber);
        var machine = SimulatedMachineTestData.Create(snapshot);
        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Failure);
        var outStart = SimulatedMachineTestData.Discovery.AddMinutes(value: 10);

        var change = machine.Apply(
            SimulatedMachineTestData.WithHistory(
                snapshot,
                SimulatedMachineTestData.Row(
                    MachineState.Operational, SimulatedMachineTestData.Now.AddDays(value: -30)),
                SimulatedMachineTestData.Row(MachineState.OutOfService, outStart),
                SimulatedMachineTestData.Row(MachineState.Operational, outStart.AddSeconds(value: 2))));

        change.Should().Be(SnapshotChange.None);
        machine.Modules[ModuleName.Spindle].Mode.Should().Be(ModuleMode.Failure);
    }

    [Fact]
    [Feature(TestFeatures.Machines)]
    public void Apply_WithMaintenanceEndBeforeDiscovery_ResetsNothing()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber);
        var machine = SimulatedMachineTestData.Create(snapshot);
        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Failure);
        var maintenanceStart = SimulatedMachineTestData.Discovery.AddDays(value: -2);

        var change = machine.Apply(
            SimulatedMachineTestData.WithHistory(
                snapshot,
                SimulatedMachineTestData.Row(
                    MachineState.Operational, SimulatedMachineTestData.Now.AddDays(value: -30)),
                SimulatedMachineTestData.Row(MachineState.InMaintenance, maintenanceStart),
                SimulatedMachineTestData.Row(MachineState.Operational, maintenanceStart.AddHours(value: 1))));

        change.Should().Be(SnapshotChange.None);
        machine.Modules[ModuleName.Spindle].Mode.Should().Be(ModuleMode.Failure);
    }

    [Fact]
    public void Tick_WithTripFailure_FreezesTheDrainOfAnotherModule()
    {
        var machine = SimulatedMachineTestData.Create(
            SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber));

        machine.TrySetMode(ModuleName.Hydraulics, ModuleMode.Draining);
        machine.Tick(SimulatedMachineTestData.Now, TimeSpan.FromSeconds(seconds: 18));
        var progress = machine.Modules[ModuleName.Hydraulics].Progress;
        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Failure);

        machine.Tick(SimulatedMachineTestData.Now, TimeSpan.FromSeconds(seconds: 18));

        progress.Should().BeGreaterThan(expected: 0);
        machine.Modules[ModuleName.Hydraulics].Progress.Should().Be(progress);
    }

    [Fact]
    public void Tick_WhileCutting_GrowsTheCounterByOneThreeThousandSixHundredthPerSecond()
    {
        var machine = SimulatedMachineTestData.Create(
            SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber));

        machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        machine.Counters["machine.operating_hours"].Should().BeApproximately(1.0 / 3600, precision: 1e-12);
    }

    [Fact]
    public void Apply_WithAnotherSerialNumber_BindsAgainAndResetsCountersAndModules()
    {
        var snapshot = SimulatedMachineTestData.Snapshot(SimulatedMachineTestData.CncSerialNumber);
        var machine = SimulatedMachineTestData.Create(snapshot);
        machine.TrySetMode(ModuleName.Spindle, ModuleMode.Failure);
        machine.Tick(SimulatedMachineTestData.Now, SimulatedMachineTestData.OneSecond);

        var change = machine.Apply(snapshot with { SerialNumber = SimulatedMachineTestData.PumpSerialNumber });

        change.Should().Be(SnapshotChange.Rebound);
        machine.ScenarioKind.Should().Be(ScenarioKind.CentrifugalPump);
        machine.Counters["pump.operating_hours"].Should().Be(expected: 12030);
        machine.Modules.Keys.Should().NotContain(ModuleName.Spindle);
    }

    [Fact]
    public void ApplyCounterStart_WithStoredReading_ReplacesTheStartValueOfThatKeyOnly()
    {
        var machine = SimulatedMachineTestData.Create(
            SimulatedMachineTestData.Snapshot(ScenarioBinding.CompressorSerialNumber));

        machine.ApplyCounterStart(
            new Dictionary<string, double> { ["running.hours"] = 100.5, ["other"] = 1 });

        machine.Counters["running.hours"].Should().Be(expected: 100.5);
        machine.Counters["loaded.hours"].Should().Be(expected: 11860);
        machine.Counters.Should().NotContainKey(unexpected: "other");
    }
}
