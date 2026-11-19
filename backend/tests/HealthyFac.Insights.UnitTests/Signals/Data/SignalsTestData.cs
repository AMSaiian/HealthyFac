using Google.Protobuf.WellKnownTypes;
using HealthyFac.Telemetry.Contracts.Signals;

namespace HealthyFac.Insights.UnitTests.Signals.Data;

public static class SignalsTestData
{
    public const string DownstreamScope = "telemetry";

    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");
    public static readonly Guid MetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000301");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid EpisodeId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000401");

    public static readonly DateTime From = new(
        year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime To = From.AddHours(value: 1);

    public static MetricHistoryReply CreateHistoryReply()
    {
        var reply = new MetricHistoryReply();
        reply.Buckets.Add(
            new MetricBucket
            {
                Start = Timestamp.FromDateTime(From),
                Avg = 15,
                Min = 10,
                Max = 20,
                Count = 2,
            });

        return reply;
    }

    public static SignalEpisodesReply CreateEpisodesReply(bool hasOptionalValues)
    {
        var episode = new SignalEpisode
        {
            EpisodeId = EpisodeId.ToString(),
            MachineId = MachineId.ToString(),
            CompanyId = CompanyId.ToString(),
            Rule = "threshold",
            Severity = "warning",
            State = "open",
            OpenedAt = Timestamp.FromDateTime(From.AddMinutes(value: 5)),
            Sequence = 1,
        };

        if (hasOptionalValues)
        {
            episode.MetricDefinitionId = MetricId.ToString();
            episode.MetricKey = "spindle.temperature";
            episode.PhaseCode = "cutting";
            episode.Value = 91;
            episode.Limit = 90;
            episode.ResolvedAt = Timestamp.FromDateTime(From.AddMinutes(value: 9));
        }

        var reply = new SignalEpisodesReply();
        reply.Episodes.Add(episode);

        return reply;
    }
}
