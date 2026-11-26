using System.Reflection;
using System.Text.RegularExpressions;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Repository;
using Xunit.v3;

namespace HealthyFac.Testing.Conventions;

[Feature(TestFeatures.Conventions)]
public sealed partial class TestTraitConventionTests
{
    private const string KindTraitName = "Kind";
    private const string ServiceTraitName = "Service";
    private const string PendingFilePath = "backend/tests/feature-audit-pending.txt";

    [Fact]
    public void Assembly_WithItsName_HasOneKindAndOneServiceTrait()
    {
        var assembly = typeof(TestTraitConventionTests).Assembly;
        var nameMatch = TestProjectNamePattern().Match(assembly.GetName().Name!);

        var traits = ReadTraits(assembly.GetCustomAttributes()).ToList();
        var kinds = traits.Where(trait => trait.Key == KindTraitName).ToList();
        var services = traits.Where(trait => trait.Key == ServiceTraitName).ToList();

        kinds.Should().ContainSingle().Which.Value.Should().Be(nameMatch.Groups["kind"].Value);
        services.Should().ContainSingle().Which.Value.Should().Be(nameMatch.Groups["service"].Value);
    }

    [Fact]
    public void TestMethods_OfTheAssembly_HaveKnownFeatureTraits()
    {
        var assembly = typeof(TestTraitConventionTests).Assembly;

        if (IsPending(assembly.GetName().Name!))
        {
            return;
        }

        var knownFeatures = TestFeatures.All.ToHashSet();

        var violations = FindTestMethods(assembly)
            .Where(method => !HasKnownFeature(method, knownFeatures))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Order()
            .ToList();

        violations.Should().BeEmpty("every test needs a Feature trait of TestFeatures");
    }

    private static List<MethodInfo> FindTestMethods(Assembly assembly) =>
    [
        .. assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<FactAttribute>(inherit: true).Any()),
    ];

    private static bool HasKnownFeature(MethodInfo method, HashSet<string> knownFeatures)
    {
        var attributes = method.GetCustomAttributes(inherit: true)
            .Concat(EnumerateTypes(method.DeclaringType!).SelectMany(type => type.GetCustomAttributes(inherit: true)))
            .Concat(method.DeclaringType!.Assembly.GetCustomAttributes());

        var features = ReadTraits(attributes).Where(trait => trait.Key == FeatureAttribute.TraitName).ToList();

        return features.Count > 0 && features.TrueForAll(trait => knownFeatures.Contains(trait.Value));
    }

    private static IEnumerable<Type> EnumerateTypes(Type type)
    {
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            yield return current;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadTraits(IEnumerable<object> attributes) =>
        attributes.OfType<ITraitAttribute>().SelectMany(attribute => attribute.GetTraits());

    private static bool IsPending(string assemblyName)
    {
        var path = Path.Combine(RepositoryRoot.Find().FullName, PendingFilePath);

        return File.Exists(path) && File.ReadAllLines(path).Select(line => line.Trim()).Contains(assemblyName);
    }

    [GeneratedRegex(@"^HealthyFac\.(?<service>.+)\.(?<kind>Unit|Architecture|Integration|Api)Tests$")]
    private static partial Regex TestProjectNamePattern();
}
