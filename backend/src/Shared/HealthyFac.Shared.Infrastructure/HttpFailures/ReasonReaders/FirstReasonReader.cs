namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

/// <summary>
/// Asks an ordered list of readers and returns the first reason that is not blank. It reads no field itself.
/// </summary>
/// <param name="readers">The readers in the order of their priority.</param>
internal sealed class FirstReasonReader(params IHttpErrorReasonReader[] readers) : IHttpErrorReasonReader
{
    private readonly IHttpErrorReasonReader[] _readers = readers;

    public string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields) =>
        _readers
            .Select(reader => reader.ReadReason(fields))
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));
}
