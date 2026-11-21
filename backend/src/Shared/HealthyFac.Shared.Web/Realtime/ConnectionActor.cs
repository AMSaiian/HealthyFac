using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Web.CurrentUser;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Builds the <see cref="ActorContext"/> of a hub connection from its user token once, and keeps it in the items of
/// the connection. The user ID provider and the hub read the same actor, so no code parses the claims again.
/// </summary>
public static class ConnectionActor
{
    private const string ItemKey = "HealthyFac.ConnectionActor";

    /// <summary>Gives the actor of the connection, and builds it on the first call.</summary>
    /// <param name="items">The items of the connection, which live as long as it does.</param>
    /// <param name="user">The user of the connection.</param>
    /// <returns>The actor of the connection.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException">
    /// Thrown when the token has no valid <c>sub</c>, <c>organization</c>, or <c>employee_id</c>. The connect ends.
    /// </exception>
    public static ActorContext Of(IDictionary<object, object?> items, ClaimsPrincipal user)
    {
        if (items.TryGetValue(ItemKey, out var existing) && existing is ActorContext actor)
        {
            return actor;
        }

        var built = ActorClaimsReader.ReadUser(user);
        items[ItemKey] = built;

        return built;
    }
}
