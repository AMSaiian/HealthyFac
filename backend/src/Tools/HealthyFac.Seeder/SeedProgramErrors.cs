namespace HealthyFac.Seeder;

/// <summary>
/// Defines the messages of the seed job entry.
/// </summary>
public static class SeedProgramErrors
{
    public const string Cancelled = "The seed job was stopped.";

    public const string Complete = "The demo seed is complete.";

    public static readonly string Incomplete =
        "The demo seed is incomplete: an earlier run stopped or failed. "
        + $"Run 'docker compose run --rm seeder {SeedProgram.ResetArgument}' to start it again.";

    public static string UnknownArgument(string argument) =>
        $"The argument {argument} is not known. The only argument is {SeedProgram.ResetArgument}.";

    public static string CheckFailed(int count) => $"The seed check found {count} failures. Nothing was written.";
}
