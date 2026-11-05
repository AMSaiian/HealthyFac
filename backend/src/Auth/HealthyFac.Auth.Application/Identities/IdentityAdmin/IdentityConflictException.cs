namespace HealthyFac.Auth.Application.Identities;

/// <summary>
/// Reports that the identity provider already holds a user with the same email. No retry can fix it.
/// </summary>
public sealed class IdentityConflictException : Exception
{
    public IdentityConflictException(string message)
        : base(message)
    {
    }
}
