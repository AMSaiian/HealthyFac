using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[CollectionDefinition(Name)]
public sealed class TelemetryEvaluationCollection : ICollectionFixture<TelemetryEvaluationFixture>
{
    public const string Name = "TelemetryEvaluation";
}
