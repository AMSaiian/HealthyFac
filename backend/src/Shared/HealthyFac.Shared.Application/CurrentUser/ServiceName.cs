using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.CurrentUser;

/// <summary>
/// Defines the deployable services. The camelCase values are part of the actor IDs and the <c>Service:Name</c>
/// setting, so never rename one in place. Add a member with each new service.
/// </summary>
public sealed class ServiceName : Enumeration<ServiceName>
{
    public static readonly ServiceName Core = new(value: "core");
    public static readonly ServiceName Auth = new(value: "auth");
    public static readonly ServiceName Gateway = new(value: "gateway");
    public static readonly ServiceName Spa = new(value: "spa");

    private ServiceName(string value)
        : base(value)
    {
    }
}
