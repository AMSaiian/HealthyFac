namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the limits of the development data settings.
/// </summary>
public static class SeedingValidationConstants
{
    public static class Messages
    {
        public const string SeedMachineIdEmpty = "A seed machine ID must not be empty.";

        public const string SeedMachineIdDuplicated = "The seed machine IDs must be distinct.";
    }
}
