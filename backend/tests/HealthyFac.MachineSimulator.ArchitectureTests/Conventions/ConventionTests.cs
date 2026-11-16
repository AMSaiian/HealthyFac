using HealthyFac.MachineSimulator.ArchitectureTests.Conventions.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.MachineSimulator.ArchitectureTests.Conventions;

public sealed class ConventionTests
{
    [Fact]
    public void OptionsClasses_InSimulator_DeclareSectionName()
    {
        var violations = ConventionChecks.FindOptionsWithoutSectionName(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void ExtensionMethods_OnSetupReceivers_UseReceiverPrefix()
    {
        var violations = ConventionChecks.FindExtensionMethodsWithWrongPrefix(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void RootFolders_InSimulator_HaveNoTechnicalName()
    {
        var violations = ConventionChecks.FindTechnicalRootFolders(
            ConventionTestData.Simulator,
            ConventionTestData.TechnicalRootFolderNames);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Booleans_InSimulator_HaveIsHasOrCanPrefix()
    {
        var violations = ConventionChecks.FindBooleansWithoutPrefix(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void CancellationTokenParameters_InSimulator_AreNamedCancellationToken()
    {
        var violations = ConventionChecks.FindCancellationTokensWithWrongName(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void ProtectedAbstractSteps_InSimulator_EndWithInternal()
    {
        var violations = ConventionChecks.FindAbstractStepsWithoutInternalSuffix(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void PrimaryConstructorParameters_InSimulator_AreNotCaptured()
    {
        var violations = ConventionChecks.FindCapturedPrimaryConstructorParameters(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Records_InSimulator_HaveNoPrimaryConstructor()
    {
        var violations = ConventionChecks.FindPositionalRecords(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void UseCaseHandlers_InSimulator_DeriveFromRequestHandler()
    {
        var violations = ConventionChecks.FindDirectUnitHandlers(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Types_InSimulator_AreNotControllers()
    {
        var violations = ConventionChecks.FindControllers(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Middleware_InSimulator_ImplementsIMiddleware()
    {
        var violations = ConventionChecks.FindConventionMiddleware(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Types_InSimulator_InjectNoConcreteDbContext()
    {
        var violations = ConventionChecks.FindDbContextInjections(ConventionTestData.Simulator);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InSimulator_LiveInMessagingAssembly()
    {
        var violations = ConventionChecks.FindMessagesOutsideMessagingAssembly(ConventionTestData.SimulatorAssemblies);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InSimulator_HaveNoCopy()
    {
        var violations = ConventionChecks.FindDuplicateMessages(ConventionTestData.SimulatorAssemblies);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }
}
