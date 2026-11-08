namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the limits of the development data settings.
/// </summary>
public static class SeedingValidationConstants
{
    public const int MaxStatusDefinitionsPerMachine = 100;

    public static class Messages
    {
        public const string SeedMachineIdEmpty = "A seed machine ID must not be empty.";

        public const string SeedMachineIdDuplicated = "The seed machine IDs must be distinct.";

        public const string SeedMachineGatewayIdInvalid =
            "A seed machine gateway ID must have the gateway ID format.";

        public const string SeedMachineGatewayIdDuplicated = "The seed machine gateway IDs must be distinct.";
    }
}
