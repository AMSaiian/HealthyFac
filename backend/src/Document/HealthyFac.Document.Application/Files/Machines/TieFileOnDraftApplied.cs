using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Document.Application.Files.Machines;

public sealed class TieFileOnDraftAppliedPreHandlerCheck : PassThroughPreHandlerCheck<MachineDraftApplied>;

/// <summary>
/// Ties the source file of a machine draft to the machine that a user created from it. A draft that a chat proposed
/// has no file, so nothing is tied. The rules are in <see cref="AutomaticFileTie"/>.
/// </summary>
/// <param name="automaticFileTie">Applies the tie.</param>
public sealed class TieFileOnDraftAppliedRequestHandler(AutomaticFileTie automaticFileTie)
    : RequestHandler<MachineDraftApplied>
{
    private readonly AutomaticFileTie _automaticFileTie = automaticFileTie;

    protected override async Task HandleInternalAsync(
        MachineDraftApplied request,
        CancellationToken cancellationToken
    )
    {
        if (request.FileId is not { } fileId || request.QueuedAt is not { } queuedAt)
        {
            return;
        }

        await _automaticFileTie.TieAsync(fileId, request.CompanyId, request.MachineId, queuedAt, cancellationToken);
    }
}
