using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

/// <summary>
/// Plays a flow with no incoming user token, like the planner run of an inbox message.
/// </summary>
public sealed class NoUserTokenAccessor : IIncomingAccessTokenAccessor
{
    public string? TryGetUserAccessToken() => null;

    public Guid? TryGetUserId() => null;
}
