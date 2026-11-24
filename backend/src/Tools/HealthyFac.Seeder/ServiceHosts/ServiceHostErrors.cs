namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// Defines the error messages of the service hosts.
/// </summary>
public static class ServiceHostErrors
{
    public const string NoMessageBus = "The seed job has no message bus; use the outbox.";

    public static string HostBuildFailed(string service, string reason) =>
        $"The host of the service {service} cannot start: {reason}";
}
