namespace HealthyFac.Seeder.Cases;

/// <summary>Defines the error messages of the cases part.</summary>
public static class CasesErrors
{
    public static string CoverageNotSupported(string item, string coverage) =>
        $"The event {item} sets the signal coverage {coverage}. The part proposes a work order draft, "
        + "so only drafted is valid.";
}
