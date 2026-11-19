using System.Collections.Frozen;
using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.QueryExpressions;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

/// <summary>The columns that the case grid sorts by.</summary>
public sealed class CaseSortColumn : Enumeration<CaseSortColumn>
{
    public static readonly CaseSortColumn OpenedAt = new(value: "openedAt");

    public static readonly CaseSortColumn State = new(value: "state");

    private CaseSortColumn(string value)
        : base(value)
    {
    }
}

/// <summary>The filter of the case grid: the states and the machines.</summary>
public sealed record CaseGridFilter : IFilter<MaintenanceCase>
{
    public SetFilter<CaseState>? States { get; init; }

    public SetFilter<Guid>? MachineIds { get; init; }

    /// <summary>The highest open severity of the case. A case without an open signal matches no value.</summary>
    public SetFilter<SignalSeverity>? Severities { get; init; }

    /// <summary>
    /// <c>true</c> keeps the cases whose last run left the AI unavailable, <c>false</c> keeps the others. The handler
    /// applies it, because it reads the runs, which are no part of the case.
    /// </summary>
    public bool? IsAiUnavailable { get; init; }

    /// <summary>
    /// Keeps the cases whose last run has one of the states. A case without a run matches no state. The handler
    /// applies it, because it reads the runs, which are no part of the case.
    /// </summary>
    public SetFilter<AgentRunState>? LastRunStates { get; init; }

    public Expression<Func<MaintenanceCase, bool>>? ToPredicate() =>
        Predicates.From(States?.ToPredicate<MaintenanceCase>(maintenanceCase => maintenanceCase.State))
            .And(MachineIds?.ToPredicate<MaintenanceCase>(maintenanceCase => maintenanceCase.MachineId))
            .And(Severities?.ToPredicate(CaseSeverityRules.HighestOpenSeverity));
}

/// <summary>The severity rule that the list row and the severity filter share, so they cannot differ.</summary>
public static class CaseSeverityRules
{
    /// <summary>The highest severity among the open signals of the case: an alarm before a warning.</summary>
    public static readonly Expression<Func<MaintenanceCase, SignalSeverity>> HighestOpenSeverity =
        maintenanceCase => maintenanceCase.Signals
            .Where(signal => signal.State == SignalState.Open)
            .OrderBy(signal => signal.Severity == SignalSeverity.Alarm ? 0 : 1)
            .Select(signal => signal.Severity)
            .FirstOrDefault()!;
}

public sealed class CaseGridFilterValidator : AbstractValidator<CaseGridFilter>
{
    public CaseGridFilterValidator()
    {
        When(filter => filter.States is not null, () => RuleFor(filter => filter.States!).IsSetFilter());

        When(filter => filter.MachineIds is not null, () => RuleFor(filter => filter.MachineIds!).IsSetFilter());

        When(filter => filter.Severities is not null, () => RuleFor(filter => filter.Severities!).IsSetFilter());

        When(filter => filter.LastRunStates is not null, () => RuleFor(filter => filter.LastRunStates!).IsSetFilter());
    }
}

/// <summary>The sort map of the case grid. The newest case comes first by default.</summary>
public static class CaseSortMap
{
    public static readonly FrozenDictionary<CaseSortColumn, Expression<Func<MaintenanceCase, dynamic>>> Instance =
        new Dictionary<CaseSortColumn, Expression<Func<MaintenanceCase, dynamic>>>
        {
            [CaseSortColumn.OpenedAt] = maintenanceCase => maintenanceCase.OpenedAt,
            [CaseSortColumn.State] = maintenanceCase => maintenanceCase.State == CaseState.Open
                ? 0
                : maintenanceCase.State == CaseState.Resolved
                    ? 1
                    : 2,
        }.ToFrozenDictionary();

    public static readonly GridSort<CaseSortColumn> Default = new()
    {
        Column = CaseSortColumn.OpenedAt,
        Direction = SortDirection.Descending,
    };
}

public record SearchCasesRequest : GridRequest<CaseGridFilter, CaseSortColumn>,
    IRequest<GridResponse<CaseListItemResponse>>
{
    public required Guid CompanyId { get; init; }
}

public sealed class SearchCasesRequestValidator
    : GridRequestValidator<SearchCasesRequest, CaseGridFilter, CaseSortColumn>
{
    public SearchCasesRequestValidator()
        : base(CaseSortColumn.All)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.Filter!)
            .SetValidator(new CaseGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchCasesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchCasesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchCasesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the cases of the company. The last run of a case is the run with the highest ID, because the IDs
/// are time ordered. The highest open severity is <c>alarm</c> when one open signal is an alarm, else <c>warning</c>
/// when one open signal exists, else none.
/// </summary>
/// <param name="dbContext">Reads the cases and their runs.</param>
public sealed class SearchCasesRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<SearchCasesRequest, GridResponse<CaseListItemResponse>>
{
    /// <summary>
    /// The AI is unavailable when the last run of the case was interrupted or failed for a provider or an unexpected
    /// reason. A run that failed because the case changed does not count.
    /// </summary>
    private static readonly Expression<Func<AgentRun, bool>> IsAiUnavailableRule = run =>
        run.State == AgentRunState.Interrupted
        || (run.State == AgentRunState.Failed
            && (run.ErrorCode == AgentRunErrorCode.ProviderUnavailable
                || run.ErrorCode == AgentRunErrorCode.UnexpectedError));

    private static Expression<Func<MaintenanceCase, SignalSeverity, CaseListItemResponse>> RowTemplate(
        IQueryable<AgentRun> runs
    ) =>
        (maintenanceCase, highestOpenSeverity) => new CaseListItemResponse
        {
            Id = maintenanceCase.Id,
            MachineId = maintenanceCase.MachineId,
            State = maintenanceCase.State,
            HighestOpenSeverity = highestOpenSeverity,
            SignalCount = maintenanceCase.Signals.Count,
            LastRunState = runs
                .Where(run => run.CaseId == maintenanceCase.Id)
                .OrderByDescending(run => run.Id)
                .Select(run => (AgentRunState?)run.State)
                .FirstOrDefault(),
            IsAiUnavailable = runs
                .Where(run => run.CaseId == maintenanceCase.Id)
                .OrderByDescending(run => run.Id)
                .Select(IsAiUnavailableRule)
                .FirstOrDefault(),
            OpenedAt = maintenanceCase.OpenedAt,
            ClosedAt = maintenanceCase.ClosedAt,
            CloseReason = maintenanceCase.CloseReason,
        };

    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<GridResponse<CaseListItemResponse>> HandleAsync(
        SearchCasesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var runs = _dbContext.AgentRuns.AsNoTracking();
        var response = await _dbContext.MaintenanceCases
            .AsNoTracking()
            .Where(maintenanceCase => maintenanceCase.CompanyId == request.CompanyId)
            .ApplyFilter(request.Filter)
            .Where(maintenanceCase => request.Filter == null
                || request.Filter.LastRunStates == null
                || request.Filter.LastRunStates.Values.Contains(
                    runs
                        .Where(run => run.CaseId == maintenanceCase.Id)
                        .OrderByDescending(run => run.Id)
                        .Select(run => run.State)
                        .FirstOrDefault()))
            .Where(maintenanceCase => request.Filter == null
                || request.Filter.IsAiUnavailable == null
                || runs
                    .Where(run => run.CaseId == maintenanceCase.Id)
                    .OrderByDescending(run => run.Id)
                    .Select(IsAiUnavailableRule)
                    .FirstOrDefault()
                == request.Filter.IsAiUnavailable)
            .ApplySort(request.Sort ?? CaseSortMap.Default, CaseSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                RowTemplate(runs).Embed(CaseSeverityRules.HighestOpenSeverity),
                cancellationToken);

        return response;
    }
}
