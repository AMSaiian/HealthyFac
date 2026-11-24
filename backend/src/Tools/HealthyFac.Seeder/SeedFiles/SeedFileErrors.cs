namespace HealthyFac.Seeder.SeedFiles;

/// <summary>
/// Defines the error messages of the seed files.
/// </summary>
public static class SeedFileErrors
{
    public static string FileMissing(string path) => $"The seed file {path} does not exist.";

    public const string EventsLieInThePast = "Events lie in the past: the moment is later than the start of the job.";

    public static string KeyNotCreated(string key) => $"No event creates the key {key}.";

    public static string KeyUsedBeforeCreated(string key, string creatorItem) =>
        $"The key {key} is used before the event {creatorItem} creates it.";

    public static string EventAfterCompletion(string completionItem) =>
        $"The event runs after the completion marker event {completionItem}. The marker event must be the last event.";

    public static string CompletionEventMissing(string completionItem) =>
        $"No event has the completion marker item {completionItem}. "
        + "The check cannot confirm that the marker event is last.";

    public static string RequestInvalid(string reason) => $"The request is not valid: {reason}";

    public static string FileInvalid(string path, string? jsonPath, string reason) =>
        $"The seed file {path} is not valid at {jsonPath ?? "$"}: {reason}";
}
