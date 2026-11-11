using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Document.Application.Files.Machines;

public sealed class TieFileOnMachineMatchedPreHandlerCheck : PassThroughPreHandlerCheck<FileMachineMatched>;

/// <summary>
/// Ties a company file to the machine whose serial number the ingest found in the file. The rules are in
/// <see cref="AutomaticFileTie"/>.
/// </summary>
/// <param name="automaticFileTie">Applies the tie.</param>
public sealed class TieFileOnMachineMatchedRequestHandler(AutomaticFileTie automaticFileTie)
    : RequestHandler<FileMachineMatched>
{
    private readonly AutomaticFileTie _automaticFileTie = automaticFileTie;

    protected override Task HandleInternalAsync(FileMachineMatched request, CancellationToken cancellationToken) =>
        _automaticFileTie.TieAsync(
            request.FileId,
            request.CompanyId,
            request.MachineId,
            request.QueuedAt,
            cancellationToken);
}
