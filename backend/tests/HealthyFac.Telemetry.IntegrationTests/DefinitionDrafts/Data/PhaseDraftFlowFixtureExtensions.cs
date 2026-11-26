using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;

/// <summary>
/// Stores a proposal on a real machine of a tenant: one metric draft, one status draft, and one phase draft that
/// uses both, all pending on the machine.
/// </summary>
public static class PhaseDraftFlowFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        /// <summary>
        /// Applies a phase draft with the reviewed values.
        /// </summary>
        /// <param name="request">The apply request.</param>
        /// <returns>The ID of the new phase.</returns>
        public async Task<Guid> ApplyPhaseDraftAsync(ApplyOperatingPhaseDraftRequest request)
        {
            using var scope = fixture.CreateUserScope(request.CompanyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<ApplyOperatingPhaseDraftRequest, ApplyOperatingPhaseDraftResponse>>();

            var response = await handler.HandleAsync(request);

            return response.Id;
        }

        /// <summary>
        /// Reads the stored phases of the machine, with their rows, also the deleted ones.
        /// </summary>
        /// <param name="machineId">The machine.</param>
        /// <returns>The phases.</returns>
        public async Task<IReadOnlyList<OperatingPhase>> LoadPhasesOfMachineAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .OperatingPhases
                .AsNoTracking()
                .Include(phase => phase.Conditions)
                .Include(phase => phase.Limits)
                .Where(phase => phase.MachineRegistrationId == machineId)
                .ToListAsync();
        }

        /// <summary>
        /// Delivers the proposal and its accepted outcome for the machine.
        /// </summary>
        /// <param name="tenant">The tenant of the machine.</param>
        /// <param name="machineId">The machine that the entry belongs to.</param>
        /// <returns>The IDs of the entry and its three drafts.</returns>
        public async Task<FlowIds> StorePendingPhaseProposalAsync(MachineTenant tenant, Guid machineId)
        {
            var ids = FlowIds.Create() with { MachineId = machineId };

            var machine = ProposalBuilder.Machine(machineId) with
            {
                DraftId = ids.EntryId,
                Metrics = [new ProposedMetric { DraftId = ids.MetricId, Key = "motor.temperature" }],
                Statuses = [new ProposedStatus { DraftId = ids.StatusId, Code = "E104" }],
                Phases = [PhaseDraftTestData.ProposedPhase(ids)],
            };

            await fixture.StoreProposedDefinitionDraftsAsync(
                tenant.CompanyId, ProposalBuilder.Message(tenant.CompanyId, machine));

            await fixture.ResolveDraftsAsync(
                new DraftsResolved
                {
                    AttemptId = DefinitionDraftTestData.AttemptId,
                    CompanyId = tenant.CompanyId,
                    Resolution = DraftResolution.Accepted,
                    Entries = [new ResolvedEntry { EntryId = ids.EntryId, MachineId = machineId }],
                });

            return ids;
        }
    }
}
