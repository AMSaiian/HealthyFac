using System.Text.Json;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

public static class EvaluationTestData
{
    public const string Topic = "healthyfac/gateways/test/messages";

    public static readonly Guid CompanyId = MachineRegistrationTestData.CompanyId;

    public static readonly DateTime Start = new(
        year: 2026, month: 11, day: 1, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly MetricLimits DefaultLimits = new() { WarningHigh = 80, AlarmHigh = 90 };

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>One value for each second, from <paramref name="start"/>.</summary>
    public static List<SeededSample> Series(
        EvaluationArrangement arrangement,
        IEnumerable<double> values,
        DateTime start,
        double stepSeconds = 1,
        params Guid[] activeStatusIds
    ) =>
    [
        .. values
            .Select((value, index) => new SeededSample(
                start.AddSeconds(index * stepSeconds),
                new Dictionary<Guid, double> { [arrangement.MetricId] = value },
                activeStatusIds)),
    ];

    public static List<SeededSample> Constant(
        EvaluationArrangement arrangement,
        double value,
        int count,
        DateTime start,
        double stepSeconds = 1,
        params Guid[] activeStatusIds
    ) =>
        Series(arrangement, Enumerable.Repeat(value, count), start, stepSeconds, activeStatusIds);

    /// <summary>The samples of consecutive segments, one for each second, from <paramref name="start"/>.</summary>
    public static List<SeededSample> Build(
        EvaluationArrangement arrangement,
        DateTime start,
        params Segment[] segments
    )
    {
        var samples = new List<SeededSample>();

        foreach (var segment in segments)
        {
            for (var index = 0; index < segment.Count; index++)
            {
                var offset = samples.Count;
                var value = segment.Mean + (segment.HasWiggle ? offset % 2 == 0 ? 0.5 : -0.5 : 0);

                samples.Add(
                    new SeededSample(
                        start.AddSeconds(offset),
                        new Dictionary<Guid, double> { [arrangement.MetricId] = value },
                        segment.ActiveStatusIds));
            }
        }

        return samples;
    }

    public static DateTime At(double seconds) => Start.AddSeconds(seconds);

    /// <summary>A flat series with a small, fixed wiggle: it has a variance and never reaches a limit.</summary>
    public static IEnumerable<double> Wiggle(double mean, int count) =>
        Enumerable.Range(start: 0, count).Select(index => mean + (index % 2 == 0 ? 0.5 : -0.5));
}

/// <summary>A run of samples with the same mean and the same active statuses.</summary>
/// <param name="Count">The number of samples, one for each second.</param>
/// <param name="Mean">The value of the samples.</param>
/// <param name="ActiveStatusIds">The statuses that are active in the samples.</param>
/// <param name="HasWiggle">Adds a fixed wiggle of 0.5 around the mean, so the series has a variance.</param>
public sealed record Segment(int Count, double Mean, Guid[] ActiveStatusIds, bool HasWiggle = false);
