using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Plays the incoming request. A test sets the user token and the user ID, or leaves them empty for a flow without
/// a user.
/// </summary>
public sealed class FakeIncomingAccessTokenAccessor : IIncomingAccessTokenAccessor
{
    public string? AccessToken { get; set; }

    public Guid? UserId { get; set; }

    public string? TryGetUserAccessToken() => AccessToken;

    public Guid? TryGetUserId() => UserId;
}
