namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Defines the limits of the development data settings. The limits keep a start-up seed small and fast.
/// </summary>
public static class SeedingValidationConstants
{
    public static class Counts
    {
        public const int MaxCompanies = 50;
        public const int MaxPerDepartment = 100;
        public const int MaxSupervisorsPerDepartment = 10;
        public const int MaxOpenInvitesPerCompany = 50;

        /// <summary>The number of departments of the test tenant. Keycloak lists the same number of IDs.</summary>
        public const int TestTenantDepartments = 2;
    }

    /// <summary>
    /// Defines the time bounds of the seeded state history of a machine. The machine faker picks the changes that
    /// follow the first <c>operational</c> row by the machine index.
    /// </summary>
    public static class StateHistory
    {
        public const int MinStartDaysBeforeNow = 30;
        public const int MaxStartDaysBeforeNow = 365;

        /// <summary>
        /// The lower bound of the share of the time between the previous row and the seed time at which the next
        /// state starts.
        /// </summary>
        public const double MinChangeShare = 0.1;

        /// <summary>
        /// The upper bound of the same share. It stays below 1, so every change starts before the seed time.
        /// </summary>
        public const double MaxChangeShare = 0.9;
    }

    public static class InviteLifetime
    {
        public const int MinDays = 1;
        public const int MaxDays = 30;
    }

    /// <summary>
    /// The length of the suffix that the machine faker appends to a machine type: a space and a two-digit number.
    /// </summary>
    public const int MachineNameSuffixLength = 3;

    public static class Messages
    {
        public const string RecreationOnlyInDevelopment =
            "Forced recreation of development data deletes business data, so it is allowed only in the Development "
            + "environment.";

        public static readonly string TestTenantDepartmentCount =
            $"The test tenant needs exactly {Counts.TestTenantDepartments} department IDs.";

        public const string TestTenantDepartmentIdEmpty = "A test tenant department ID must not be empty.";

        public const string TestTenantDepartmentIdDuplicated = "The test tenant department IDs must be distinct.";
    }
}
