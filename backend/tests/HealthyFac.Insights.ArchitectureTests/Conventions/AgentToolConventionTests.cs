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

    [Fact]
    public void AgentTools_OfTheChatRoleWithoutPermissionOrActivity_AreReported()
    {
        var violations =
            AgentToolConventionData.FindViolations([typeof(NoPermissionChatTool), typeof(NoActivityChatTool)]);

        violations.Should().HaveCount(expected: 2);
        violations.Should().Contain(text => text.Contains(nameof(NoPermissionChatTool)) && text.Contains("permission"));
        violations.Should().Contain(text => text.Contains(nameof(NoActivityChatTool)) && text.Contains("activity"));
    }

    [Fact]
    public void AgentTools_OfTheChatRoleWithDownstreamScopes_AreReported()
    {
        var violations = AgentToolConventionData.FindViolations([typeof(ScopedChatTool)]);

        violations.Should().ContainSingle().Which.Should().Contain(expected: "DownstreamScopes");
    }

    [Fact]
    public void AgentTools_OfTheChatRoleWithTheNoAnswerName_NeedNoPermissionAndNoActivity()
    {
        var violations = AgentToolConventionData.FindViolations([typeof(NoAnswerChatTool)]);

        violations.Should().BeEmpty();
    }

    [AgentTool(role: "chat", name: "no_permission", Activity = "machines")]
    private sealed class NoPermissionChatTool;

    [AgentTool(role: "chat", name: "no_activity", Permissions = ["machines:read"])]
    private sealed class NoActivityChatTool;

    [AgentTool(role: "chat", name: "scoped_chat", Permissions = ["machines:read"], Activity = "machines")]
    [DownstreamScopes("machines:read")]
    private sealed class ScopedChatTool;

    [AgentTool(role: "chat", name: "report_no_answer")]
    private sealed class NoAnswerChatTool;

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
