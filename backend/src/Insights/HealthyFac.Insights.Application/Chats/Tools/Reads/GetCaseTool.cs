using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetCaseArguments
{
    public required Guid CaseId { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Insights.CasesRead],
    Activity = "cases")]
public sealed record GetCaseToolRequest
    : IAgentToolRequest<GetCaseToolRequest, GetCaseArguments, GetCaseResponse>
{
    public const string ToolName = "get_case";

    public const string ToolDescription =
        "Reads one maintenance case by its ID: its state, its outcome (the kind, the title, the reason), and its "
        + "signals (the rule, the severity, the state, the metric or status, and the times).";

    public required GetCaseArguments Arguments { get; init; }

    public static GetCaseToolRequest Create(GetCaseArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetCaseResponse
{
    public required ChatCase Case { get; init; }

    public required string OutcomeKind { get; init; }

    public string? OutcomeTitle { get; init; }

    public string? OutcomeReason { get; init; }

    public Guid? WorkOrderId { get; init; }

    public required IReadOnlyList<ChatCaseSignal> Signals { get; init; }
}

public sealed record ChatCaseSignal
{
    public required Guid Id { get; init; }

    public required string Rule { get; init; }

    public required string Severity { get; init; }

    public required string State { get; init; }

    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }
}

public sealed class GetCaseArgumentsValidator : AbstractValidator<GetCaseArguments>
{
    public GetCaseArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.CaseId);
    }
}

public sealed class GetCaseToolRequestValidator : AbstractValidator<GetCaseToolRequest>
{
    public GetCaseToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetCaseArgumentsValidator());
    }
}

public sealed class GetCaseToolPreHandlerCheck : PassThroughPreHandlerCheck<GetCaseToolRequest>;

/// <summary>
/// Runs the case use cases with the company of the user, so their validation and their scope check apply. A case of
/// another company is "not found" to the model.
/// </summary>
/// <param name="cases">The decorated handler of one case.</param>
/// <param name="signals">The decorated handler of the signals of a case.</param>
/// <param name="currentUser">Provides the company of the user.</param>
public sealed class GetCaseToolRequestHandler(
    IRequestHandler<GetCaseRequest, CaseDetailResponse> cases,
    IRequestHandler<SearchCaseSignalsRequest, GridResponse<CaseSignalResponse>> signals,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCaseToolRequest, GetCaseResponse>
{
    private readonly IRequestHandler<GetCaseRequest, CaseDetailResponse> _cases = cases;
    private readonly IRequestHandler<SearchCaseSignalsRequest, GridResponse<CaseSignalResponse>> _signals = signals;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<GetCaseResponse> HandleAsync(
        GetCaseToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var companyId = _currentUser.GetCompanyId();

        var detail = await _cases.HandleAsync(
            new GetCaseRequest { CompanyId = companyId, Id = request.Arguments.CaseId },
            cancellationToken);

        var signalPage = await _signals.HandleAsync(
            new SearchCaseSignalsRequest
            {
                CompanyId = companyId,
                CaseId = request.Arguments.CaseId,
                Offset = 0,
                Limit = GridValidationConstants.MaxLimit,
            },
            cancellationToken);

        return new GetCaseResponse
        {
            Case = new ChatCase
            {
                Id = detail.Id,
                MachineId = detail.MachineId,
                State = detail.State.Value,
                HighestOpenSeverity = detail.HighestOpenSeverity?.Value,
                SignalCount = detail.SignalCount,
                OpenedAt = detail.OpenedAt,
                ClosedAt = detail.ClosedAt,
                CloseReason = detail.CloseReason,
            },
            OutcomeKind = detail.Outcome.Kind.Value,
            OutcomeTitle = detail.Outcome.Title,
            OutcomeReason = detail.Outcome.Reason,
            WorkOrderId = detail.Outcome.WorkOrderId,
            Signals = [.. signalPage.Items.Select(ToSignal)],
        };
    }

    private static ChatCaseSignal ToSignal(CaseSignalResponse signal) =>
        new()
        {
            Id = signal.Id,
            Rule = signal.Rule.Value,
            Severity = signal.Severity.Value,
            State = signal.State.Value,
            MetricKey = signal.MetricKey,
            StatusCode = signal.StatusCode,
            Value = signal.Value,
            Limit = signal.Limit,
            OpenedAt = signal.OpenedAt,
            ResolvedAt = signal.ResolvedAt,
        };
}
