namespace HealthyFac.Seeder;

/// <summary>
/// Defines the messages of the seed job entry.
/// </summary>
public static class SeedProgramErrors
{
    public const string Cancelled = "The seed job was stopped.";

    public static string UnknownArgument(string argument) =>
        $"The argument {argument} is not known. The only argument is {SeedProgram.ResetArgument}.";

    public static string CheckFailed(int count) => $"The seed check found {count} failures. Nothing was written.";
}
