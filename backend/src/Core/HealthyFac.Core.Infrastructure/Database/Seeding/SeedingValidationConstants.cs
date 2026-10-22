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
    }
}
