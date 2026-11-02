namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Gives an outgoing call the access token of the user who started the current HTTP request. Only a token with the
/// user shape counts: <c>sub</c>, exactly one <c>organization</c>, and <c>employee_id</c>. A service token has
/// another shape, so a service that a service token called never treats that token as a user token.
/// </summary>
public interface IIncomingAccessTokenAccessor
{
    /// <summary>
    /// Attempts to read the bearer token of the current request.
    /// </summary>
    /// <returns>The access token of the user. <see langword="null"/> outside an HTTP request, for a request
    /// without a signed-in user, and for a service token.</returns>
    string? TryGetUserAccessToken();

    /// <summary>
    /// Attempts to read the ID of the user of the token that <see cref="TryGetUserAccessToken"/> returns.
    /// </summary>
    /// <returns>The <c>sub</c> claim of the user. <see langword="null"/> in the same cases as
    /// <see cref="TryGetUserAccessToken"/>.</returns>
    Guid? TryGetUserId();
}
