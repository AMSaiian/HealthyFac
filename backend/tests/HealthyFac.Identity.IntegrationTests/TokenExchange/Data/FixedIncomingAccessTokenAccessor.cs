using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

/// <summary>
/// Plays the incoming request of a user, so the handler under test needs no web host.
/// </summary>
/// <param name="accessToken">The access token of the user.</param>
/// <param name="userId">The ID of the user.</param>
public sealed class FixedIncomingAccessTokenAccessor(string accessToken, Guid userId) : IIncomingAccessTokenAccessor
{
    private readonly string _accessToken = accessToken;
    private readonly Guid _userId = userId;

    public string? TryGetUserAccessToken() => _accessToken;

    public Guid? TryGetUserId() => _userId;
}
