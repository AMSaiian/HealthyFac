using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Shared.UnitTests.HttpFailures.Data;

public sealed class FixedReasonReader(string? reason) : IHttpErrorReasonReader
{
    private readonly string? _reason = reason;

    public string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields) => _reason;
}
