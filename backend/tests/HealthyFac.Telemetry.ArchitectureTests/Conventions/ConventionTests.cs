using HealthyFac.Telemetry.ArchitectureTests.Conventions.Data;
using HealthyFac.Telemetry.ArchitectureTests.Dependencies.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Telemetry.ArchitectureTests.Conventions;

public sealed class ConventionTests
{
    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void OptionsClasses_InAssembly_DeclareSectionName(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindOptionsWithoutSectionName(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void ExtensionMethods_OnSetupReceivers_UseReceiverPrefix(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindExtensionMethodsWithWrongPrefix(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void RootFolders_InAssembly_HaveNoTechnicalName(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindTechnicalRootFolders(
                LayerDependencyTestData.Resolve(assemblyName),
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
            Environment.NewLine,
            ConventionChecks.FindBooleansWithoutPrefix(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void CancellationTokenParameters_InAssembly_AreNamedCancellationToken(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindCancellationTokensWithWrongName(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void ProtectedAbstractSteps_InAssembly_EndWithInternal(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindAbstractStepsWithoutInternalSuffix(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void PrimaryConstructorParameters_InAssembly_AreNotCaptured(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindCapturedPrimaryConstructorParameters(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Records_InAssembly_HaveNoPrimaryConstructor(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindPositionalRecords(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void UseCaseHandlers_WithoutResult_DeriveFromRequestHandler(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindDirectUnitHandlers(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Types_InAssembly_AreNotControllers(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine, ConventionChecks.FindControllers(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.AllAssemblies), MemberType = typeof(ConventionTestData))]
    public void Middleware_InAssembly_ImplementsIMiddleware(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindConventionMiddleware(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ConventionTestData.ApplicationAssemblies), MemberType = typeof(ConventionTestData))]
    public void ApplicationTypes_InAssembly_InjectNoConcreteDbContext(string assemblyName)
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindDbContextInjections(LayerDependencyTestData.Resolve(assemblyName)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationStrings_InAssemblies_HaveTextRule()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindStringsWithoutTextRule(
                ConventionTestData.UseCaseAssemblies.Select(LayerDependencyTestData.Resolve)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void UseCaseParts_InAssemblies_ShareTheRequestNamespace()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindUseCasePartsOutsideRequestNamespace(
                ConventionTestData.UseCaseAssemblies.Select(LayerDependencyTestData.Resolve)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InServiceAssemblies_LiveInMessagingAssembly()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindMessagesOutsideMessagingAssembly(
                ConventionTestData.MessageAssemblies.Select(LayerDependencyTestData.Resolve)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InServiceAssemblies_HaveNoCopy()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindDuplicateMessages(
                ConventionTestData.MessageAssemblies.Select(LayerDependencyTestData.Resolve)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void MessagingAssemblies_InServiceAssemblies_ReferenceNoServiceLayer()
    {
        var violations = string.Join(
            Environment.NewLine,
            ConventionChecks.FindMessagingServiceDependencies(
                ConventionTestData.MessageAssemblies.Select(LayerDependencyTestData.Resolve)));

        violations.Should().BeEmpty();
    }
}
