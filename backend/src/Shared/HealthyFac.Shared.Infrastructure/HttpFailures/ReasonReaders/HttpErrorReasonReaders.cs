namespace HealthyFac.Shared.Infrastructure.HttpFailures.ReasonReaders;

public static class HttpErrorReasonReaders
{
    /// <summary>
    /// The reader of every outgoing call. It is the only place that sets the order of the formats: ProblemDetails
    /// first (our services), then the Keycloak <c>errorMessage</c> (more specific than the OAuth fields in the admin
    /// API), then OAuth 2.0. The readers hold no state, so one instance serves every caller.
    /// </summary>
    public static readonly IHttpErrorReasonReader Default = new FirstReasonReader(
        new ProblemDetailsReasonReader(),
        new KeycloakErrorReasonReader(),
        new OAuthErrorReasonReader());
}
