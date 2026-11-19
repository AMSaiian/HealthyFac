using System.Text.Json;
using HealthyFac.Core.Messaging.WorkOrders;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Cases.Data;

/// <summary>
/// Reads the case header, its signals, its runs, and the tool calls of a run as a user of one company.
/// </summary>
public static class CaseReadFixtureExtensions
{
    private const int IdTickMilliseconds = 2;

    extension(InsightsFixture fixture)
    {
        public async Task<CaseDetailResponse> GetCaseUseCaseAsync(Guid companyId, Guid caseId)
        {
            using var scope = fixture.CreateCaseReaderScope(companyId);

            return await scope.ServiceProvider.GetRequiredService<IRequestHandler<GetCaseRequest, CaseDetailResponse>>()
                .HandleAsync(new GetCaseRequest { CompanyId = companyId, Id = caseId });
        }

        public async Task<GridResponse<CaseSignalResponse>> SearchCaseSignalsUseCaseAsync(
            Guid companyId,
            Guid caseId,
            int offset,
            int limit
        )
        {
            using var scope = fixture.CreateCaseReaderScope(companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchCaseSignalsRequest, GridResponse<CaseSignalResponse>>>()
                .HandleAsync(
                    new SearchCaseSignalsRequest
                    {
                        CompanyId = companyId,
                        CaseId = caseId,
                        Offset = offset,
                        Limit = limit,
                    });
        }

        public async Task<GridResponse<CaseRunResponse>> SearchCaseRunsUseCaseAsync(
            Guid companyId,
            Guid caseId,
            int offset,
            int limit
        )
        {
            using var scope = fixture.CreateCaseReaderScope(companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchCaseRunsRequest, GridResponse<CaseRunResponse>>>()
                .HandleAsync(
                    new SearchCaseRunsRequest
                        { CompanyId = companyId, CaseId = caseId, Offset = offset, Limit = limit });
        }

        public async Task<CaseToolCallsResponse> GetRunToolCallsUseCaseAsync(Guid companyId, Guid caseId, Guid runId)
        {
            using var scope = fixture.CreateCaseReaderScope(companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetRunToolCallsRequest, CaseToolCallsResponse>>()
                .HandleAsync(new GetRunToolCallsRequest { CompanyId = companyId, CaseId = caseId, RunId = runId });
        }

        /// <summary>
        /// Stores a case with one signal for each given state and returns the signal IDs in that order.
        /// </summary>
        public async Task<List<Guid>> SaveCaseWithSignalsAsync(
            MaintenanceCase maintenanceCase,
            params (SignalState State, int MinutesAfterStart)[] signals
        )
        {
            var ids = new List<Guid>();

            foreach (var (state, minutes) in signals)
            {
                var signal = new MachineSignal(
                    Guid.CreateVersion7(),
                    maintenanceCase.MachineId,
                    maintenanceCase.CompanyId,
                    new SignalChange
                    {
                        Rule = SignalRule.Threshold,
                        Severity = SignalSeverity.Warning,
                        State = state,
                        OpenedAt = CaseTestData.Now.AddMinutes(minutes),
                        ResolvedAt = state == SignalState.Resolved ? CaseTestData.Now.AddMinutes(minutes + 1) : null,
                        Sequence = 1,
                    });
                maintenanceCase.Add(signal);
                ids.Add(signal.Id);
            }

            await fixture.SaveAsync(maintenanceCase);

            return ids;
        }

        /// <summary>
        /// Stores one completed run of the role after a short pause, so the run IDs rise in call order.
        /// </summary>
        public async Task<AgentRun> SaveCompletedRunAsync(
            Guid caseId,
            AgentRole role,
            AgentRunVerdict? verdict,
            string? reason
        )
        {
            await Task.Delay(IdTickMilliseconds);
            var run = new AgentRun(role, caseId, ingestAttemptId: null, CaseTestData.Now);
            run.Claim(CaseTestData.Now, CaseTestData.Now.AddMinutes(value: 5));
            run.Complete(CaseTestData.Now, verdict, reason, inputTokens: 0, outputTokens: 0);
            await fixture.SaveAsync(run);

            return run;
        }

        public async Task<Guid> SaveCommittedProposalAsync(Guid runId, string title)
        {
            var action = new AgentAction(
                runId,
                CaseTestData.CallId,
                AgentActionKind.ProposeWorkOrder,
                JsonSerializer.Serialize(
                    new ProposeWorkOrderPayload
                    {
                        SignalIds = [],
                        Title = title,
                        Priority = "high",
                        Category = "corrective",
                        EstimateMinutes = 60,
                    },
                    JsonSerializerOptions.Web));
            action.Commit();
            await fixture.SaveAsync(action);

            return action.Id;
        }

        public async Task HandleDraftAppliedAsync(WorkOrderDraftApplied message)
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext
                    { Kind = ActorKind.User, UserId = Guid.CreateVersion7(), CompanyId = message.CompanyId });

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<WorkOrderDraftApplied>>()
                .HandleAsync(message);
        }

        public async Task HandleDraftDismissedAsync(WorkOrderDraftDismissed message)
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext
                    { Kind = ActorKind.User, UserId = Guid.CreateVersion7(), CompanyId = message.CompanyId });

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<WorkOrderDraftDismissed>>()
                .HandleAsync(message);
        }

        private IServiceScope CreateCaseReaderScope(Guid companyId) =>
            fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = Guid.CreateVersion7(), CompanyId = companyId });
    }
}
