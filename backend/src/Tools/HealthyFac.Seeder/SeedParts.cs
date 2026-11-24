using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.WorkOrders;

namespace HealthyFac.Seeder;

/// <summary>Lists the parts of the seed in the order of their files.</summary>
public static class SeedParts
{
    /// <summary>Lists the parts.</summary>
    /// <param name="hosts">The service hosts that the parts write through.</param>
    /// <param name="people">The people part. The company zone comes from its file.</param>
    /// <param name="isTelemetry">
    /// <see langword="true"/> for mode 2: the telemetry history part runs before <see cref="MachinesPart"/>, so a
    /// history step runs before a state change of the same moment in <c>machines.json</c>.
    /// </param>
    /// <returns>The parts. Each cycle adds its part here.</returns>
    public static IReadOnlyList<ISeedPart> Create(
        ServiceHosts.ServiceHosts hosts,
        PeoplePart people,
        bool isTelemetry
    ) =>
        isTelemetry
            ? [people, new IngestPart(hosts), new TelemetryHistoryPart(hosts), new MachinesPart(hosts), .. Tail(hosts)]
            : [people, new IngestPart(hosts), new MachinesPart(hosts), .. Tail(hosts)];

    private static ISeedPart[] Tail(ServiceHosts.ServiceHosts hosts) =>
        [new CasesPart(hosts), new WorkOrdersPart(hosts)];
}
