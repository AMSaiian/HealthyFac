using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.Following;

public sealed class FollowAppliedMachineDraftPreHandlerCheck : PassThroughPreHandlerCheck<MachineDraftApplied>;

/// <summary>
/// Moves the definition drafts that wait on a create machine draft to the machine that a user created from it. The
/// message can arrive before the outcome of the machine check, so an entry that does not exist yet is created.
/// </summary>
/// <param name="dbContext">Saves the entry and drafts.</param>
/// <param name="proposalEntries">Finds the entry and moves its drafts.</param>
public sealed class FollowAppliedMachineDraftRequestHandler(
    ITelemetryDbContext dbContext,
    ProposalEntries proposalEntries
) : RequestHandler<MachineDraftApplied>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ProposalEntries _proposalEntries = proposalEntries;

    protected override async Task HandleInternalAsync(MachineDraftApplied request, CancellationToken cancellationToken)
    {
        var entry = await _proposalEntries.FindOrCreateAsync(request.DraftId, request.CompanyId, cancellationToken);
        entry.ApplyMachineDraft(request.MachineId);
        await _proposalEntries.FollowAsync(entry, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
