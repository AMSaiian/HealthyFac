using System.Text.Json.Serialization;
using HealthyFac.Seeder.ArchitectureTests.Conventions.Data;
using HealthyFac.Testing.Conventions;

namespace HealthyFac.Seeder.ArchitectureTests.Conventions;

public sealed class ConventionTests
{
    [Fact]
    public void OptionsClasses_InSeeder_DeclareSectionName()
    {
        var violations = ConventionChecks.FindOptionsWithoutSectionName(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void ExtensionMethods_OnSetupReceivers_UseReceiverPrefix()
    {
        var violations = ConventionChecks.FindExtensionMethodsWithWrongPrefix(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void RootFolders_InSeeder_HaveNoTechnicalName()
    {
        var violations = ConventionChecks.FindTechnicalRootFolders(
            ConventionTestData.Seeder,
            ConventionTestData.TechnicalRootFolderNames);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Booleans_InSeeder_HaveIsHasOrCanPrefix()
    {
        var violations = ConventionChecks.FindBooleansWithoutPrefix(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void CancellationTokenParameters_InSeeder_AreNamedCancellationToken()
    {
        var violations = ConventionChecks.FindCancellationTokensWithWrongName(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void ProtectedAbstractSteps_InSeeder_EndWithInternal()
    {
        var violations = ConventionChecks.FindAbstractStepsWithoutInternalSuffix(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void PrimaryConstructorParameters_InSeeder_AreNotCaptured()
    {
        var violations = ConventionChecks.FindCapturedPrimaryConstructorParameters(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Records_InSeeder_HaveNoPrimaryConstructor()
    {
        var violations = ConventionChecks.FindPositionalRecords(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void UseCaseHandlers_InSeeder_DeriveFromRequestHandler()
    {
        var violations = ConventionChecks.FindDirectUnitHandlers(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Types_InSeeder_AreNotControllers()
    {
        var violations = ConventionChecks.FindControllers(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Middleware_InSeeder_ImplementsIMiddleware()
    {
        var violations = ConventionChecks.FindConventionMiddleware(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Types_InSeeder_InjectNoConcreteDbContext()
    {
        var violations = ConventionChecks.FindDbContextInjections(ConventionTestData.Seeder);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InSeeder_LiveInMessagingAssembly()
    {
        var violations = ConventionChecks.FindMessagesOutsideMessagingAssembly(ConventionTestData.SeederAssemblies);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void IntegrationMessages_InSeeder_HaveNoCopy()
    {
        var violations = ConventionChecks.FindDuplicateMessages(ConventionTestData.SeederAssemblies);

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }

    [Fact]
    public void Types_InSeeder_HaveNoJsonPropertyName()
    {
        var violations = ConventionChecks.FindMembersWithAttribute(
            ConventionTestData.Seeder,
            typeof(JsonPropertyNameAttribute));

        string.Join(Environment.NewLine, violations).Should().BeEmpty();
    }
}
