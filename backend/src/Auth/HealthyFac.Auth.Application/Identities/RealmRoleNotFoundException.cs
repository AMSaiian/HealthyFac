namespace HealthyFac.Auth.Application.Identities;

/// <summary>
/// Reports that the realm has no role with the code of the message. No retry can fix it until an admin adds the
/// role.
/// </summary>
public sealed class RealmRoleNotFoundException : Exception
{
    public RealmRoleNotFoundException(string message)
        : base(message)
    {
    }
}
