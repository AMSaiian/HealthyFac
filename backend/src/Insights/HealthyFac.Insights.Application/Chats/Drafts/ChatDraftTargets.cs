using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// Finds the buffer that a propose tool of the chat adds to. The <c>machineId</c> of the model is the ID of a create
/// draft of this turn, or the ID of an existing machine that the user may read. Any other ID is rejected to the model
/// with <see cref="ChatToolErrors.NotFoundOrNotAllowed"/>.
/// </summary>
/// <param name="buffer">Holds the proposals of the turn.</param>
/// <param name="reads">Reads an existing machine as the user.</param>
/// <param name="unitCatalog">Reads the unit catalog of the metrics.</param>
public sealed class ChatDraftTargets(ChatDraftBuffer buffer, IChatReads reads, IMetricUnitCatalog unitCatalog)
{
    private readonly ChatDraftBuffer _buffer = buffer;
    private readonly IChatReads _reads = reads;
    private readonly IMetricUnitCatalog _unitCatalog = unitCatalog;

    /// <summary>
    /// Gets the buffer of the create drafts.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the unit catalog read.</param>
    /// <returns>The buffer in the mode of a company file.</returns>
    public async Task<ExtractionBuffer> GetCreateBufferAsync(CancellationToken cancellationToken)
    {
        var units = await _unitCatalog.GetAllAsync(cancellationToken);

        return _buffer.GetCreateBuffer(units);
    }

    /// <summary>
    /// Finds the buffer and the machine entry for the <c>machineId</c> of a definition tool.
    /// </summary>
    /// <param name="machineId">The ID that the model gave.</param>
    /// <param name="cancellationToken">The token that cancels the reads.</param>
    /// <returns>The buffer, and the ID that the buffer knows the machine by.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.NotFoundException">
    /// Thrown with <see cref="ChatToolErrors.NotFoundOrNotAllowed"/> when the ID is no create draft of this turn and
    /// the user may not read a machine with it.
    /// </exception>
    public async Task<(ExtractionBuffer Buffer, Guid MachineKey)> ResolveAsync(
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        if (_buffer.IsCreateDraft(machineId))
        {
            return (await GetCreateBufferAsync(cancellationToken), machineId);
        }

        var existing = await ResolveExistingAsync(machineId, cancellationToken);
        existing.ContainerId ??= existing.Buffer.AddMachine(name: null, serialNumber: null, description: null);

        return (existing.Buffer, existing.ContainerId.Value);
    }

    /// <summary>
    /// Finds the buffer of an existing machine, and adds it when the turn did not touch the machine yet.
    /// </summary>
    /// <param name="machineId">The ID of the existing machine.</param>
    /// <param name="cancellationToken">The token that cancels the reads.</param>
    /// <returns>The entry of the machine.</returns>
    /// <exception cref="HealthyFac.Shared.Application.ErrorHandling.NotFoundException">
    /// Thrown with <see cref="ChatToolErrors.NotFoundOrNotAllowed"/> when the user may not read the machine.
    /// </exception>
    public async Task<ChatDraftBuffer.ExistingMachine> ResolveExistingAsync(
        Guid machineId,
        CancellationToken cancellationToken
    )
    {
        var known = _buffer.FindExisting(machineId);
        if (known is not null)
        {
            return known;
        }

        var machine = await _reads.GetMachineAsync(
            new GetMachineArguments { MachineId = machineId },
            cancellationToken);
        var units = await _unitCatalog.GetAllAsync(cancellationToken);
        var signals = new MachineSignals
        {
            Metrics = [.. machine.Metrics.Select(metric => new SignalReference { Id = metric.Id, Key = metric.Key })],
            Statuses =
                [.. machine.Statuses.Select(status => new SignalReference { Id = status.Id, Key = status.Code })],
        };

        return _buffer.AddExisting(machineId, units, signals);
    }
}
