using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>An accessor of a request that holds the token of a user.</summary>
public sealed class UserTokenAccessor : IIncomingAccessTokenAccessor
{
    public string? TryGetUserAccessToken() => "user-token";

    public Guid? TryGetUserId() => Guid.CreateVersion7();

    public string? TryGetUserName() => "Ann Owner";
}
