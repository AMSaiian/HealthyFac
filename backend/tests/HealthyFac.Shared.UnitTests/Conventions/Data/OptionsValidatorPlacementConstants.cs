namespace HealthyFac.Shared.UnitTests.Conventions.Data;

internal static class OptionsValidatorPlacementConstants
{
    public const string SolutionFileName = "HealthyFac.slnx";

    public const string BackendFolderName = "backend";

    public const string SourceFolderName = "src";

    public const string SourcePattern = "*.cs";

    public const string ValidatorFileSuffix = "OptionsValidator.cs";

    public const string ValidatorPattern = @"\bclass\s+\w+\s*:\s*AbstractValidator<\s*(?<options>\w+Options)\s*>";

    public static readonly string[] SkippedFolders = ["bin", "obj", "Migrations"];
}
