using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.ArchitectureTests.Conventions.Data;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.ArchitectureTests.Conventions;

public sealed class AgentToolConventionTests
{
    [Fact]
    public void AgentTools_InTheApplicationAssembly_DeclareScopesOrAreLocalAndHaveOneNameEach()
    {
        var violations = AgentToolConventionData.FindViolations(InsightsApplicationAssembly.Instance.GetTypes());

        violations.Should().BeEmpty();
    }

    [Fact]
    public void AgentTools_WithoutScopesAndWithoutTheLocalMark_AreReported()
    {
        var violations = AgentToolConventionData.FindViolations([typeof(UnmarkedTool)]);

        violations.Should().ContainSingle().Which.Should().Contain(nameof(UnmarkedTool));
    }

    [Fact]
    public void AgentTools_WithTheSameNameInTwoRoles_AreReported()
    {
        var violations = AgentToolConventionData.FindViolations([typeof(LocalPlannerTool), typeof(LocalExtractorTool)]);

        violations.Should().ContainSingle().Which.Should().Contain(expected: "shared_name");
    }

    [Fact]
    public void AgentTools_WithScopesOrTheLocalMark_AreNotReported()
    {
        var violations = AgentToolConventionData.FindViolations([typeof(ScopedTool), typeof(LocalPlannerTool)]);

        violations.Should().BeEmpty();
    }

    [AgentTool(role: "planner", name: "unmarked")]
    private sealed class UnmarkedTool;

    [AgentTool(role: "planner", name: "scoped")]
    [DownstreamScopes("work-orders:read")]
    private sealed class ScopedTool;

    [AgentTool(role: "planner", name: "shared_name")]
    [LocalTool]
    private sealed class LocalPlannerTool;

    [AgentTool(role: "extractor", name: "shared_name")]
    [LocalTool]
    private sealed class LocalExtractorTool;
}
