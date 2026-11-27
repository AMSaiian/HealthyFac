using System.Text.Json.Nodes;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.MetricDrafts;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.Proposals;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;

public static class DefinitionDraftInputFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public async Task<EncryptedDismissedDraft> ArrangeDismissedMetricDraftAsync()
        {
            var encryptedMachineId = await fixture.ArrangeMachineAsync();
            var machineId = fixture.IdEncryptor.Decrypt(encryptedMachineId);
            var entry = new ProposalEntry(Guid.CreateVersion7(), fixture.CompanyId);
            var proposal = DraftProposal.ForIngest(Guid.CreateVersion7(), fixture.CompanyId, Guid.CreateVersion7());

            var draft = MetricDefinitionDraft.Propose(
                Guid.CreateVersion7(),
                fixture.CompanyId,
                proposal.Id,
                entry.Id,
                machineId,
                new MetricDraftValues { Key = "spindle.temperature", Name = "Spindle temperature" });

            entry.AcceptOnMachine(machineId);
            draft.Follow(entry, isTargetRegistrationDeleted: false);
            draft.Dismiss();

            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            dbContext.DraftProposals.Add(proposal);
            dbContext.ProposalEntries.Add(entry);
            dbContext.MetricDefinitionDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return new EncryptedDismissedDraft(encryptedMachineId, fixture.IdEncryptor.Encrypt(draft.Id));
        }

        public string ApplyBodyWithDraft(string encryptedDraftId)
        {
            var body = JsonNode.Parse(MetricDefinitionApiTestData.CreateBody)!;
            body["created"]![0]!["draftId"] = encryptedDraftId;

            return body.ToJsonString();
        }
    }
}

public sealed record EncryptedDismissedDraft(string MachineId, string DraftId);
