namespace HealthyFac.Shared.Infrastructure.Revocation;

/// <summary>
/// Defines the Redis keys of the revocation list.
/// </summary>
public static class RevocationKeys
{
    private const string Prefix = "revoked:";

    /// <summary>
    /// Builds the key of a user. The writer (Auth service) and the reader (gateway) use this one definition.
    /// </summary>
    /// <param name="userId">The Keycloak user ID.</param>
    /// <returns>The key, for example <c>revoked:0190f3a0-0000-7000-8000-000000000002</c>.</returns>
    public static string For(Guid userId) => $"{Prefix}{userId:D}";
}
