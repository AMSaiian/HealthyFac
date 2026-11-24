using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Domain.MetricUnits;

namespace HealthyFac.Seeder.Reset;

/// <summary>
/// Lists the entities whose tables the reset keeps, because the initializer of the service seeds them as required data
/// and nothing in the seed creates them: the roles of Core and the quantity kinds and measurement units of Telemetry.
/// </summary>
public static class TruncateKeepList
{
    private static readonly IReadOnlySet<Type> None = new HashSet<Type>();

    /// <summary>Gives the entity types that the reset keeps for one service.</summary>
    /// <param name="service">The service.</param>
    /// <returns>The entity types. The set is empty for a service without required data.</returns>
    public static IReadOnlySet<Type> For(ServiceName service)
    {
        if (service == ServiceName.Core)
        {
            return new HashSet<Type> { typeof(Role) };
        }

        return service == ServiceName.Telemetry
            ? new HashSet<Type> { typeof(QuantityKind), typeof(MeasurementUnit) }
            : None;
    }
}
