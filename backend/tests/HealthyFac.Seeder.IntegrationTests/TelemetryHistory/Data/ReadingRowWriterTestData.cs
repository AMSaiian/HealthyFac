using System.Collections;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Machines.Snapshots;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.TelemetryHistory.Persistence;
using HealthyFac.Seeder.TelemetryHistory.Simulation;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

public static class ReadingRowWriterTestData
{
    public const string LatheKey = "lathe.machine";
    public static readonly Guid MarkerId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000d101");
    public static readonly TimeSpan Later = TimeSpan.FromMinutes(minutes: 10);

    /// <summary>
    /// The first sample of a lathe simulation that starts 1 hour before the anchor of the people seed.
    /// </summary>
    /// <param name="data">What Telemetry holds about the lathe.</param>
    /// <param name="machineId">The ID of the lathe.</param>
    /// <returns>The sample.</returns>
    public static HistorySample CreateLatheSample(HistoryMachineData data, Guid machineId)
    {
        var anchor = PeopleTestData.Anchor;
        var start = anchor.UtcDateTime.AddHours(value: -1);
        var snapshot = new MachineSnapshot
        {
            MachineId = machineId,
            CompanyId = data.CompanyId,
            Name = "Lathe",
            SerialNumber = ScenarioBinding.CncSerialNumber,
            GatewayId = data.GatewayId,
            ExpectedGoLiveOn = data.ExpectedGoLiveOn,
            Zone = TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId),
            StateHistory =
            [
                new StateHistoryEntry { State = MachineState.Operational, StartedAt = start.AddDays(value: -30) },
            ],
            IsRegistered = true,
            Definitions = data.Definitions,
        };
        var events = new HistoryEvents(
            [], [], [], [], anchor, TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId));
        var simulation = new HistorySimulation(
            snapshot, start, TimeSpan.FromSeconds(seconds: 1), new Random(Seed: 1), [], events);

        return simulation.Run(start, start.AddSeconds(value: 1))[index: 0];
    }

    public static HistoryMachineData WithoutStatuses(HistoryMachineData data) =>
        new()
        {
            RegistrationId = data.RegistrationId,
            CompanyId = data.CompanyId,
            GatewayId = data.GatewayId,
            ExpectedGoLiveOn = data.ExpectedGoLiveOn,
            SchemaRevision = data.SchemaRevision,
            SchemaJson = data.SchemaJson,
            MetricIdsByKey = data.MetricIdsByKey,
            StatusDefinitions = [],
            Definitions = data.Definitions,
        };

    public static string RemoveSampledAt(string payload)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(payload)!.AsObject();
        node.Remove(propertyName: "sampledAt");

        return node.ToJsonString();
    }

    /// <summary>A list that cancels a token when the writer starts to read it.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="inner">The items.</param>
    /// <param name="stop">The source to cancel.</param>
    public sealed class CancellingList<T>(IReadOnlyList<T> inner, CancellationTokenSource stop) : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _inner = inner;
        private readonly CancellationTokenSource _stop = stop;

        public int Count => _inner.Count;

        public T this[int index] => _inner[index];

        public IEnumerator<T> GetEnumerator()
        {
            _stop.Cancel();

            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
