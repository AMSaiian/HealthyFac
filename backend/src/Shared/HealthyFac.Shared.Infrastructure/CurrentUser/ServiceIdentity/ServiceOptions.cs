using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.CurrentUser;

public sealed class ServiceOptions
{
    public const string SectionName = "Service";

    /// <summary>
    /// The value of a <see cref="Application.CurrentUser.ServiceName"/> member. The processors, the start-up, and
    /// the telemetry read the name of the running service from here.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Resolves <see cref="Name"/> to its <see cref="Application.CurrentUser.ServiceName"/> member.
    /// </summary>
    /// <returns>The member of the running service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Name"/> is not a member. The validator
    /// rejects such a name at start-up.</exception>
    public ServiceName ToServiceName() => ServiceName.FromValue(Name);
}
