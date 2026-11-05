namespace HealthyFac.Shared.Infrastructure.HttpFailures;

/// <summary>
/// Reads the reason of an OAuth 2.0 error response (RFC 6749, section 5.2). With both fields, the reason is
/// <c>error: error_description</c> (e.g., <c>invalid_grant: Invalid user credentials</c>), because the code is the
/// stable part that a person can search for. With one field, the reason is that field.
/// </summary>
/// <remarks>
/// The field names are private constants, because Shared.Infrastructure does not reference
/// <c>Microsoft.IdentityModel.Protocols.OpenIdConnect</c>, and 2 names do not justify the package.
/// </remarks>
internal sealed class OAuthErrorReasonReader : IHttpErrorReasonReader
{
    private const string ErrorDescriptionField = "error_description";
    private const string ErrorField = "error";

    public string? ReadReason(IReadOnlyDictionary<string, JsonTopLevelField> fields)
    {
        var error = JsonReasonFields.ReadText(fields, ErrorField);
        var description = JsonReasonFields.ReadText(fields, ErrorDescriptionField);

        return error is not null && description is not null ? $"{error}: {description}" : description ?? error;
    }
}
