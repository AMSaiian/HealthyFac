namespace HealthyFac.Seeder.Moments;

/// <summary>
/// Defines the error messages of the relative moments. Each message names the text of the moment.
/// </summary>
public static class MomentErrors
{
    public static string InvalidForm(string text) =>
        $"The moment '{text}' is not valid. Use 'wd±N HH:mm', 'd±N HH:mm', 'd±N', or 'h±N'.";

    public static string TimeRequired(string text) => $"The moment '{text}' needs a time (HH:mm).";

    public static string DateRequired(string text) => $"The moment '{text}' has no calendar date.";

    public static string TimeDoesNotExist(string text, DateOnly date) =>
        $"The moment '{text}' lies on {date:yyyy-MM-dd}, where this local time does not exist (clock change).";

    public static string TimeIsAmbiguous(string text, DateOnly date) =>
        $"The moment '{text}' lies on {date:yyyy-MM-dd}, where this local time occurs twice (clock change).";
}
