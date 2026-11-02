namespace HealthyFac.Testing.Conventions;

public static class ConventionConstants
{
    public const string ProjectAssemblyPrefix = "HealthyFac.";
    public const string UnitAdapterType = "HealthyFac.Shared.Application.UseCases.UnitRequestHandlerAdapter`1";

    public static class Options
    {
        public const string TypeSuffix = "Options";
        public const string SectionNameField = "SectionName";
    }

    public static class ExtensionPrefixes
    {
        public const string Registration = "Add";
        public const string Pipeline = "Use";
        public const string Endpoints = "Map";
    }

    public static class StepNames
    {
        public const string Suffix = "Internal";
        public const string AsyncSuffix = "InternalAsync";
    }

    public static class CompilerNames
    {
        public const string Marker = "<";
        public const string RecordPrintMembers = "PrintMembers";
        public const string Deconstruct = "Deconstruct";
        public const string CapturedParameterPattern = "^<(?<name>.+)>P$";
        public const string CapturedParameterGroup = "name";
    }

    public static class CancellationTokens
    {
        public const string ParameterName = "cancellationToken";
    }

    public static class Booleans
    {
        public static readonly IReadOnlyList<string> Prefixes = ["is", "has", "can"];

        public const char FieldPrefix = '_';
    }
}
