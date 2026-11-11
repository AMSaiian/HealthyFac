using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Application.Authorization;

/// <summary>
/// Tells which rows of a feature the actor may reach. The Web layer resolves it from the permissions of the token and
/// sets it on the Application request, so a client never sends it. <see cref="Company"/> reaches every row of the
/// company. <see cref="Own"/> reaches the rows of the employee of the actor only.
/// </summary>
public sealed class AccessScope : Enumeration<AccessScope>
{
    public static readonly AccessScope Company = new(value: "company");
    public static readonly AccessScope Own = new(value: "own");

    private AccessScope(string value)
        : base(value)
    {
    }
}
