namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Defines the validator messages for the service actor configuration.
/// </summary>
public static class ServiceActorValidationMessages
{
    private static readonly string Setting =
        $"{ServiceActorOptions.SectionName}:{nameof(ServiceActorOptions.AllowedClients)}";

    public static readonly string ClientBlank = $"A client ID in {Setting} must not be blank.";

    public static readonly string ClientDuplicated = $"{Setting} must list each client ID only once.";
}
