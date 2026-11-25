namespace HealthyFac.Seeder;

/// <summary>
/// Defines the messages of the seed job entry.
/// </summary>
public static class SeedProgramErrors
{
    public const string Cancelled = "The seed job was stopped.";

    public const string Complete = "The demo seed is complete.";

    public static readonly string Incomplete = IncompleteFor(SeedProgram.ResetArgument);

    public static readonly string IncompleteWithTelemetry =
        IncompleteFor($"{SeedProgram.ResetArgument} {SeedProgram.TelemetryArgument}");

    public static readonly string NoCompleteHistory =
        "The demo seed has no complete telemetry history. "
        + $"Run: docker compose run --rm seeder {SeedProgram.ResetArgument} {SeedProgram.TelemetryArgument} "
        + "(stop core, telemetry, document, insights, and auth first).";

    public static string UnknownArgument(string argument) =>
        $"The argument {argument} is not known. The known arguments are {SeedProgram.ResetArgument} "
        + $"and {SeedProgram.TelemetryArgument}.";

    private static string IncompleteFor(string arguments) =>
        "The demo seed is incomplete: an earlier run stopped or failed. "
        + $"Run 'docker compose run --rm seeder {arguments}' to start it again.";

    public static string CheckFailed(int count) => $"The seed check found {count} failures. Nothing was written.";
}
