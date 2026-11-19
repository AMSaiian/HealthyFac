using HealthyFac.Insights.Infrastructure.Agents.Persistence;
using HealthyFac.Insights.Infrastructure.Cases.Persistence;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.IntegrationTests.Cases;

[Collection(InsightsCollection.Name)]
public sealed class CaseConstraintTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task Save_SecondOpenCaseOfMachine_FailsOnOpenMachineIndex()
    {
        await _fixture.SaveAsync(CaseTestData.CreateOpenCase());

        var act = () => _fixture.SaveAsync(CaseTestData.CreateOpenCase());

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(CaseIndexNames.OpenMachine));
    }

    [Fact]
    public async Task Save_OpenCaseNextToResolvedCaseOfMachine_Succeeds()
    {
        await _fixture.SaveAsync(CaseTestData.CreateResolvedCase());

        var act = () => _fixture.SaveAsync(CaseTestData.CreateOpenCase());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_OpenCaseOfAnotherMachine_Succeeds()
    {
        await _fixture.SaveAsync(CaseTestData.CreateOpenCase());

        var act = () => _fixture.SaveAsync(CaseTestData.CreateOpenCase(CaseTestData.OtherMachineId));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_SecondActionWithTheSameRunAndCallId_FailsOnActionCallIndex()
    {
        var run = CaseTestData.CreateRun();
        await _fixture.SaveAsync(run, CaseTestData.CreateAction(run.Id));

        var act = () => _fixture.SaveAsync(CaseTestData.CreateAction(run.Id));

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(AgentIndexNames.ActionCall));
    }

    [Fact]
    public async Task Save_ActionWithTheSameCallIdInAnotherRun_Succeeds()
    {
        var run = CaseTestData.CreateRun();
        var otherRun = CaseTestData.CreateRun();
        await _fixture.SaveAsync(run, otherRun, CaseTestData.CreateAction(run.Id));

        var act = () => _fixture.SaveAsync(CaseTestData.CreateAction(otherRun.Id));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Save_SecondToolCallAtTheSamePositionOfRun_FailsOnSequenceIndex()
    {
        var run = CaseTestData.CreateRun();
        await _fixture.SaveAsync(run, CaseTestData.CreateToolCall(run.Id, sequence: 1));

        var act = () => _fixture.SaveAsync(CaseTestData.CreateToolCall(run.Id, sequence: 1));

        await act.Should()
            .ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException!.Message.Contains(AgentIndexNames.ToolCallSequence));
    }
}
