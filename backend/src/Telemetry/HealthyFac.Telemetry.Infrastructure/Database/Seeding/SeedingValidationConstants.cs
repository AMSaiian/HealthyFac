namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the limits of the development data settings.
/// </summary>
public static class SeedingValidationConstants
{
    public const int MaxStatusDefinitionsPerMachine = 100;

    /// <summary>The number of departments of the test tenant. Core and Keycloak list the same number of IDs.</summary>
    public const int TestTenantDepartments = 2;

    public static class Messages
    {
        public static readonly string TestTenantDepartmentCount =
            $"The test tenant needs exactly {TestTenantDepartments} department IDs.";

        public const string TestTenantDepartmentIdEmpty = "A test tenant department ID must not be empty.";

        public const string TestTenantDepartmentIdDuplicated = "The test tenant department IDs must be distinct.";

        public const string SeedMachineIdEmpty = "A seed machine ID must not be empty.";

        public const string SeedMachineIdDuplicated = "The seed machine IDs must be distinct.";

        public const string SeedMachineGatewayIdInvalid =
            "A seed machine gateway ID must have the gateway ID format.";

        public const string SeedMachineGatewayIdDuplicated = "The seed machine gateway IDs must be distinct.";

        public const string SeedMachineDepartmentIndexOutOfRange =
            "A seed machine department index must point into the test tenant department IDs.";
    }
}
