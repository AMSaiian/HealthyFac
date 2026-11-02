using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the kinds of actor that perform an operation. The camelCase values are part of the actor IDs, so never
/// rename one in place.
/// </summary>
public sealed class ActorKind : Enumeration<ActorKind>
{
    public static readonly ActorKind User = new("user");
    public static readonly ActorKind Service = new("service");
    public static readonly ActorKind Anonymous = new("anonymous");

    private ActorKind(string value)
        : base(value)
    {
    }
}
