using System.Reflection;
using HealthyFac.Core.ArchitectureTests.Conventions.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Core.ArchitectureTests.Conventions;

public sealed class ConventionTests
{
    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void OptionsClasses_InAssembly_DeclareSectionName(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindOptionsWithoutSectionName(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void ExtensionMethods_OnSetupReceivers_UseReceiverPrefix(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindExtensionMethodsWithWrongPrefix(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void RootFolders_InAssembly_HaveNoTechnicalName(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindTechnicalRootFolders(
                Assembly.Load(assemblyName),
                ConventionTestData.TechnicalRootFolderNames
            )
        );

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Booleans_InAssembly_HaveIsHasOrCanPrefix(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindBooleansWithoutPrefix(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void ProtectedAbstractSteps_InAssembly_EndWithInternal(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindAbstractStepsWithoutInternalSuffix(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void PrimaryConstructorParameters_InAssembly_AreNotCaptured(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindCapturedPrimaryConstructorParameters(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Records_InAssembly_HaveNoPrimaryConstructor(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindPositionalRecords(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void UseCaseHandlers_WithoutResult_DeriveFromRequestHandler(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindDirectUnitHandlers(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Types_InAssembly_AreNotControllers(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindControllers(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Middleware_InAssembly_ImplementsIMiddleware(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindConventionMiddleware(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.ApplicationAssemblies), MemberType = typeof(ConventionTestData))]
    public void ApplicationTypes_InAssembly_InjectNoConcreteDbContext(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindDbContextInjections(Assembly.Load(assemblyName)));

        violations.Should().BeEmpty();
    }
}
