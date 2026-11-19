using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.DefinitionDrafts.Following;
using HealthyFac.Telemetry.Domain.DefinitionDrafts.ProposalEntries;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

public sealed class ResolveDefinitionDraftsPreHandlerCheck : PassThroughPreHandlerCheck<DraftsResolved>;

/// <summary>
/// Applies the resolution of a proposal that Core sent. Each entry takes its outcome (rejected, on a machine, or on a
/// create machine draft), and its definition drafts follow. The message can arrive before the proposal, so an entry
/// that does not exist yet is created. The handler never throws to wait for another message. It follows the entries in
/// ascending machine ID order, so 2 messages lock their registrations in one order.
/// </summary>
/// <param name="dbContext">Saves the entries and drafts.</param>
/// <param name="proposalEntries">Finds the entries and moves their drafts.</param>
public sealed class ResolveDefinitionDraftsRequestHandler(
    ITelemetryDbContext dbContext,
    ProposalEntries proposalEntries
) : RequestHandler<DraftsResolved>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ProposalEntries _proposalEntries = proposalEntries;

    protected override async Task HandleInternalAsync(DraftsResolved request, CancellationToken cancellationToken)
    {
        var entries = new List<ProposalEntry>();

        foreach (var resolved in request.Entries)
        {
            var entry = await _proposalEntries.FindOrCreateAsync(
                resolved.EntryId, request.CompanyId, cancellationToken);

            if (request.Resolution == DraftResolution.Rejected)
            {
                entry.Reject();
            }
            else if (resolved.MachineId is { } machineId)
            {
                entry.AcceptOnMachine(machineId);
            }
            else
            {
                entry.AcceptOnMachineDraft();
            }

            entries.Add(entry);
        }

        foreach (var entry in entries.OrderBy(entry => entry.MachineId))
        {
            await _proposalEntries.FollowAsync(entry, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
