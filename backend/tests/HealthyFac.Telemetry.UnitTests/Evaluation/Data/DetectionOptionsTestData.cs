using HealthyFac.Telemetry.Application.Evaluation;

namespace HealthyFac.Telemetry.UnitTests.Evaluation.Data;

public static class DetectionOptionsTestData
{
    public static TheoryData<string, DetectionOptions, string> BrokenCrossRules => new()
    {
        {
            "fast tau equal to slow tau",
            CreateValid() with { FastTauSeconds = 1800 },
            DetectionMessages.FastTauNotBelowSlowTau
        },
        {
            "fast tau above slow tau",
            CreateValid() with { FastTauSeconds = 1900 },
            DetectionMessages.FastTauNotBelowSlowTau
        },
        {
            "drift resolve limit equal to drift limit",
            CreateValid() with { DriftResolveLimit = 3 },
            DetectionMessages.DriftResolveLimitNotBelowDriftLimit
        },
        {
            "phase dwell equal to unknown mode dwell",
            CreateValid() with { PhaseDwellSeconds = 60 },
            DetectionMessages.PhaseDwellNotBelowUnknownModeDwell
        },
    };

    /// <summary>The values of <c>appsettings.json</c>.</summary>
    public static DetectionOptions CreateValid() =>
        new()
        {
            EvaluationIntervalSeconds = 10,
            SettleLagSeconds = 5,
            WarmUpSamples = 60,
            SlowTauSeconds = 1800,
            FastTauSeconds = 20,
            DriftLimit = 3,
            DriftResolveLimit = 2,
            LevelTauSeconds = 30,
            TrendTauSeconds = 60,
            HorizonSeconds = 900,
            NoiseGuardFactor = 3,
            Persistence = 2,
            GapSeconds = 600,
            SigmaFloorShare = 0.01,
            CatchUpBatchReadings = 10000,
            SilenceLimitSeconds = 120,
            PhaseDwellSeconds = 15,
            UnknownModeDwellSeconds = 60,
            SettleTimeSeconds = 30,
            LeaseSeconds = 60,
            HistoryBucketCap = 500,
        };
}
