using System.Text.Json;
using FluentValidation.Results;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Application.Chats.Links;

/// <summary>
/// Resolves the links of a new message as the user, before anything is stored. Each link loads through the read of its
/// kind (the same reads as the chat tools), so each service applies the permissions and the own scope of the user. The
/// reads of the links run in parallel; the reads of cases share one database context and run one after the other. A
/// link that does not exist, or that the user may not read, rejects the send with the failure key
/// <c>links[&lt;index&gt;]</c>, and nothing is stored.
/// </summary>
/// <param name="reads">Reads machines, work orders, and employees from Core and Telemetry.</param>
/// <param name="files">Reads a file from Document.</param>
/// <param name="cases">Reads a case with the company of the user.</param>
/// <param name="currentUser">Provides the company of the user.</param>
/// <param name="json">Writes the context of a link with encrypted IDs.</param>
public sealed class ChatLinkResolver(
    IChatReads reads,
    IFileCatalog files,
    IRequestHandler<GetCaseRequest, CaseDetailResponse> cases,
    ICurrentUserProvider currentUser,
    IAgentJson json
)
{
    private const string CaseLabelFormat = "Case of {0:yyyy-MM-dd HH:mm}";
    private const string CaseMachineLabelFormat = "{1}, case of {0:yyyy-MM-dd HH:mm}";

    private readonly IChatReads _reads = reads;
    private readonly IFileCatalog _files = files;
    private readonly IRequestHandler<GetCaseRequest, CaseDetailResponse> _cases = cases;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IAgentJson _json = json;
    private readonly SemaphoreSlim _caseGate = new(initialCount: 1);

    /// <summary>
    /// Resolves the links.
    /// </summary>
    /// <param name="links">The links of the message in the order of the request.</param>
    /// <param name="cancellationToken">The token that cancels the reads.</param>
    /// <returns>The resolved links in the order of the request.</returns>
    /// <exception cref="AppValidationException">
    /// Thrown when a link does not exist or the user may not read it. Each failure names its link as
    /// <c>links[&lt;index&gt;]</c>.
    /// </exception>
    public async Task<IReadOnlyList<ResolvedLink>> ResolveAsync(
        IReadOnlyList<ChatLinkInput> links,
        CancellationToken cancellationToken
    )
    {
        var resolved = await Task.WhenAll(links.Select(link => ResolveOneAsync(link, cancellationToken)));

        var failures = resolved
            .Select((link, index) => (Link: link, Index: index))
            .Where(item => item.Link is null)
            .Select(item => new ValidationFailure($"links[{item.Index}]", ChatErrors.LinkNotReadable))
            .ToList();
        if (failures.Count > 0)
        {
            throw new AppValidationException(failures);
        }

        return resolved.Select(link => link!).ToList();
    }

    private async Task<ResolvedLink?> ResolveOneAsync(ChatLinkInput link, CancellationToken cancellationToken)
    {
        try
        {
            var (label, context) = await ReadAsync(link, cancellationToken);
            if (label is null)
            {
                return null;
            }

            return new ResolvedLink
            {
                Kind = link.Kind,
                EntityId = link.EntityId,
                Position = link.Position,
                Label = Cut(label),
                ContextBlock = ToolDataBlocks.Wrap(
                    JsonSerializer.Serialize(new { kind = link.Kind.Value, data = context }, _json.Tools)),
            };
        }
        catch (Exception exception) when (exception is NotFoundException or ForbiddenAccessException)
        {
            return null;
        }
    }

    private async Task<(string? Label, object? Context)> ReadAsync(
        ChatLinkInput link,
        CancellationToken cancellationToken
    )
    {
        if (link.Kind == ChatLinkKind.Machine)
        {
            var machine = await _reads.GetMachineAsync(
                new GetMachineArguments { MachineId = link.EntityId },
                cancellationToken);

            return (machine.Machine.Name, machine);
        }

        if (link.Kind == ChatLinkKind.WorkOrder)
        {
            var workOrder = await _reads.GetWorkOrderAsync(
                new GetWorkOrderArguments { WorkOrderId = link.EntityId },
                cancellationToken);

            return (workOrder.Title, workOrder);
        }

        if (link.Kind == ChatLinkKind.Employee)
        {
            var employee = await _reads.FindEmployeeAsync(link.EntityId, cancellationToken);

            return (employee?.DisplayName, employee);
        }

        if (link.Kind == ChatLinkKind.File)
        {
            var file = await _files.GetFileForUserAsync(link.EntityId, cancellationToken);

            return (file?.FileName, file is null ? null : ToFileContext(file));
        }

        return await ReadCaseAsync(link.EntityId, cancellationToken);
    }

    private async Task<(string? Label, object? Context)> ReadCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken
    )
    {
        await _caseGate.WaitAsync(cancellationToken);
        try
        {
            var detail = await _cases.HandleAsync(
                new GetCaseRequest { CompanyId = _currentUser.GetCompanyId(), Id = caseId },
                cancellationToken);

            var machineName = await FindMachineNameAsync(detail.MachineId, cancellationToken);

            return (
                string.Format(
                    machineName is null ? CaseLabelFormat : CaseMachineLabelFormat, detail.OpenedAt, machineName),
                new
                {
                    id = detail.Id,
                    machineId = detail.MachineId,
                    state = detail.State.Value,
                    highestOpenSeverity = detail.HighestOpenSeverity?.Value,
                    signalCount = detail.SignalCount,
                    openedAt = detail.OpenedAt,
                    outcome = detail.Outcome.Kind.Value,
                    outcomeReason = detail.Outcome.Reason,
                });
        }
        finally
        {
            _caseGate.Release();
        }
    }

    private async Task<string?> FindMachineNameAsync(Guid machineId, CancellationToken cancellationToken)
    {
        try
        {
            var machine = await _reads.GetMachineAsync(
                new GetMachineArguments { MachineId = machineId },
                cancellationToken);

            return machine.Machine.Name;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static object ToFileContext(IngestFile file) =>
        new
        {
            fileId = file.FileId,
            fileName = file.FileName,
            extension = file.Extension,
            sizeBytes = file.SizeBytes,
            machineId = file.MachineId,
        };

    private static string Cut(string label) =>
        label.Length <= ChatMessageLink.LabelMaxLength
            ? label
            : char.IsHighSurrogate(label[ChatMessageLink.LabelMaxLength - 1])
                ? label[..(ChatMessageLink.LabelMaxLength - 1)]
                : label[..ChatMessageLink.LabelMaxLength];
}
