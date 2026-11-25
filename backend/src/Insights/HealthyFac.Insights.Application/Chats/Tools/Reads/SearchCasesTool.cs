using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchCasesArguments
{
    public string[]? States { get; init; }

    public Guid[]? MachineIds { get; init; }

    public string[]? Severities { get; init; }

    public int? Limit { get; init; }

    public int? Offset { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Insights.CasesRead],
    Activity = "cases")]
public sealed record SearchCasesToolRequest
    : IAgentToolRequest<SearchCasesToolRequest, SearchCasesArguments, SearchCasesResponse>
{
    public const string ToolName = "search_cases";

    public const string ToolDescription =
        "Searches the maintenance cases of the company by state (open, resolved, closed), machine, and the highest "
        + "open severity (warning, alarm), the newest case first. Use limit and offset to page.";

    public required SearchCasesArguments Arguments { get; init; }

    public static SearchCasesToolRequest Create(SearchCasesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchCasesResponse
{
    public required int TotalCount { get; init; }

    public required IReadOnlyList<ChatCase> Cases { get; init; }
}

public sealed record ChatCase
{
    public required Guid Id { get; init; }

    public required Guid MachineId { get; init; }

    public required string State { get; init; }

    public string? HighestOpenSeverity { get; init; }

    public required int SignalCount { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public string? CloseReason { get; init; }
}

public sealed class SearchCasesArgumentsValidator : AbstractValidator<SearchCasesArguments>
{
    public SearchCasesArgumentsValidator()
    {
        this.AddPagingRules(arguments => arguments.Limit, arguments => arguments.Offset);

        RuleForEach(arguments => arguments.States!)
            .SingleLineText()
            .Must(state => CaseState.TryFromValue(state, out _))
            .When(arguments => arguments.States is not null)
            .WithMessage(ChatToolErrors.CaseStateUnknown);

        RuleForEach(arguments => arguments.Severities!)
            .SingleLineText()
            .Must(severity => SignalSeverity.TryFromValue(severity, out _))
            .When(arguments => arguments.Severities is not null)
            .WithMessage(ChatToolErrors.SeverityUnknown);
    }
}

public sealed class SearchCasesToolRequestValidator : AbstractValidator<SearchCasesToolRequest>
{
    public SearchCasesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchCasesArgumentsValidator());
    }
}

public sealed class SearchCasesToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchCasesToolRequest>;

/// <summary>
/// Runs the case grid use case with the company of the user, so its validation and its scope check apply.
/// </summary>
/// <param name="cases">The decorated handler of the case grid.</param>
/// <param name="currentUser">Provides the company of the user.</param>
public sealed class SearchCasesToolRequestHandler(
    IRequestHandler<SearchCasesRequest, GridResponse<CaseListItemResponse>> cases,
    ICurrentUserProvider currentUser
) : IRequestHandler<SearchCasesToolRequest, SearchCasesResponse>
{
    private readonly IRequestHandler<SearchCasesRequest, GridResponse<CaseListItemResponse>> _cases = cases;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<SearchCasesResponse> HandleAsync(
        SearchCasesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var arguments = request.Arguments;
        var page = await _cases.HandleAsync(
            new SearchCasesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Offset = arguments.Offset ?? 0,
                Limit = arguments.Limit ?? GridValidationConstants.MaxLimit,
                Sort = CaseSortMap.Default,
                Filter = new CaseGridFilter
                {
                    States = arguments.States is null
                        ? null
                        : new SetFilter<CaseState> { Values = arguments.States.Select(CaseState.FromValue).ToList() },
                    MachineIds = arguments.MachineIds is null
                        ? null
                        : new SetFilter<Guid> { Values = arguments.MachineIds },
                    Severities = arguments.Severities is null
                        ? null
                        : new SetFilter<SignalSeverity>
                        {
                            Values = arguments.Severities.Select(SignalSeverity.FromValue).ToList(),
                        },
                },
            },
            cancellationToken);

        return new SearchCasesResponse { TotalCount = page.TotalCount, Cases = page.Items.Select(ToCase).ToList() };
    }

    private static ChatCase ToCase(CaseListItemResponse item) =>
        new()
        {
            Id = item.Id,
            MachineId = item.MachineId,
            State = item.State.Value,
            HighestOpenSeverity = item.HighestOpenSeverity?.Value,
            SignalCount = item.SignalCount,
            OpenedAt = item.OpenedAt,
            ClosedAt = item.ClosedAt,
            CloseReason = item.CloseReason,
        };
}
