using Xunit.v3;

namespace HealthyFac.Testing.Features;

/// <summary>
/// Tags a test, a test class, or a test assembly with a feature of <c>test-features.json</c>. The run script selects
/// tests by this trait (<c>--filter-trait "Feature=..."</c>).
/// </summary>
/// <param name="name">The feature name. Use a constant of <see cref="TestFeatures"/>.</param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class FeatureAttribute(string name) : Attribute, ITraitAttribute
{
    public const string TraitName = "Feature";

    private readonly string _name = name;

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new(TraitName, _name)];
}
