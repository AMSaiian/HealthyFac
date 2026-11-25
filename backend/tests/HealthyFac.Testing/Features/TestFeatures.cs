using System.Reflection;

namespace HealthyFac.Testing.Features;

/// <summary>
/// The feature names of <c>test-features.json</c>, in the same order. <c>TestFeatureListTests</c> checks the list.
/// </summary>
public static class TestFeatures
{
    public const string Identity = "Identity";
    public const string Companies = "Companies";
    public const string Machines = "Machines";
    public const string WorkOrders = "WorkOrders";
    public const string Shifts = "Shifts";
    public const string Files = "Files";
    public const string Ingest = "Ingest";
    public const string Drafts = "Drafts";
    public const string SignalModel = "SignalModel";
    public const string Intake = "Intake";
    public const string Evaluation = "Evaluation";
    public const string Cases = "Cases";
    public const string Chat = "Chat";
    public const string Realtime = "Realtime";
    public const string Gateway = "Gateway";
    public const string Messaging = "Messaging";
    public const string Persistence = "Persistence";
    public const string Platform = "Platform";
    public const string Seeding = "Seeding";
    public const string Simulation = "Simulation";
    public const string Conventions = "Conventions";

    /// <summary>The names of the constants above, in their order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        .. typeof(TestFeatures)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .OrderBy(field => field.MetadataToken)
            .Select(field => (string)field.GetRawConstantValue()!),
    ];
}
