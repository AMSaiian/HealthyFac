namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

/// <summary>
/// Reads the reason of a failed call from the fields of an error body. One implementation reads one error format.
/// </summary>
public interface IHttpErrorReasonReader
{
    /// <summary>
    /// Picks the reason from an error body.
    /// </summary>
    /// <param name="fields">The string fields of the root object of the error body, by name.</param>
    /// <returns>The reason, or <see langword="null"/> when the body has none of the fields of this format.</returns>
    string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields);
}
