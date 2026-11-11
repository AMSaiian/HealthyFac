using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Document.Application.Files.Machines;

public sealed class TieFileOnDraftAppliedPreHandlerCheck : PassThroughPreHandlerCheck<MachineDraftApplied>;

/// <summary>
/// Ties the source file of a machine draft to the machine that a user created from it. The rules are in
/// <see cref="AutomaticFileTie"/>.
/// </summary>
/// <param name="automaticFileTie">Applies the tie.</param>
public sealed class TieFileOnDraftAppliedRequestHandler(AutomaticFileTie automaticFileTie)
    : RequestHandler<MachineDraftApplied>
{
    private readonly AutomaticFileTie _automaticFileTie = automaticFileTie;

    protected override Task HandleInternalAsync(MachineDraftApplied request, CancellationToken cancellationToken) =>
        _automaticFileTie.TieAsync(
            request.FileId,
            request.CompanyId,
            request.MachineId,
            request.QueuedAt,
            cancellationToken);
}
