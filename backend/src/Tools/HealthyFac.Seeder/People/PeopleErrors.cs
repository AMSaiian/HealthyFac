namespace HealthyFac.Seeder.People;

/// <summary>
/// Defines the error messages of the people part.
/// </summary>
public static class PeopleErrors
{
    public const string RoleMissing = "The role of the invite does not exist in Core.";

    public static string InviteMissing(Guid inviteId) => $"The invite {inviteId} does not exist in Core.";

    public static string AcceptNeedsTime(string item) => $"The event {item} needs a moment with a time of day.";

    public static string AcceptTooLate(string item) =>
        $"The event {item} lies too late in the day: the shift event 30 minutes later would pass midnight.";

    public static string PersonUnknown(string key) => $"The seed has no person with the key {key}.";
}
