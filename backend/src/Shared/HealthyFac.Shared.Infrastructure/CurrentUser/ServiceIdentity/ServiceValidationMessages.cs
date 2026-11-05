using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;

/// <summary>
/// Defines the validator messages for the service configuration.
/// </summary>
public static class ServiceValidationMessages
{
    public static readonly string NameInvalid =
        "The service name must be one of: "
        + $"{string.Join(separator: ", ", ServiceName.All.Select(service => service.Value))}.";
}
