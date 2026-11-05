namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

/// <summary>
/// Reads the reason of a Keycloak admin <c>ErrorRepresentation</c>: <c>errorMessage</c> only. The reader never reads
/// <c>params</c> or <c>field</c>, because a Keycloak validation error repeats the input value in <c>params</c> (for
/// example, the email address of a person).
/// </summary>
internal sealed class KeycloakErrorReasonReader : IHttpErrorReasonReader
{
    private const string ErrorMessageField = "errorMessage";

    public string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields) =>
        JsonReasonFields.ReadText(fields, ErrorMessageField);
}
