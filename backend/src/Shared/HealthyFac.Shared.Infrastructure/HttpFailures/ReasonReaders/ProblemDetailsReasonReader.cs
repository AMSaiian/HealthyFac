namespace HealthyFac.Shared.Infrastructure.HttpFailures;

/// <summary>
/// Reads the reason of a ProblemDetails body (RFC 9457), the error format of our services: <c>detail</c>, else
/// <c>title</c>.
/// </summary>
internal sealed class ProblemDetailsReasonReader : IHttpErrorReasonReader
{
    private const string DetailField = "detail";
    private const string TitleField = "title";

    public string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields) =>
        JsonReasonFields.ReadText(fields, DetailField) ?? JsonReasonFields.ReadText(fields, TitleField);
}
