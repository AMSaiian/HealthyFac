using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

public record ListMetricDefinitionsRequest : IRequest<ListMetricDefinitionsResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

/// <summary>
/// The active metric definitions of a machine with the current revision, which the person sends back as
/// <c>ExpectedRevision</c> when they apply changes.
/// </summary>
public record ListMetricDefinitionsResponse
{
    public required int Revision { get; init; }

    public required IReadOnlyList<MetricDefinitionResponse> Definitions { get; init; }
}

public record MetricDefinitionResponse
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required MetricValueKind ValueKind { get; init; }

    public required MetricUnitResponse Unit { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public record MetricUnitResponse
{
    public required string QuantityKind { get; init; }

    public required string Name { get; init; }

    public required string Abbreviation { get; init; }
}

public class ListMetricDefinitionsRequestValidator : AbstractValidator<ListMetricDefinitionsRequest>
{
    public ListMetricDefinitionsRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.MachineIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, a
/// deleted machine, and an unknown machine are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ListMetricDefinitionsPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<ListMetricDefinitionsRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ListMetricDefinitionsRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Lists the active metric definitions of a machine, oldest first, with the current revision. The pre-handler check
/// has already checked the machine scope.
/// </summary>
/// <param name="dbContext">Reads the definitions and the revision.</param>
public sealed class ListMetricDefinitionsRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<ListMetricDefinitionsRequest, ListMetricDefinitionsResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ListMetricDefinitionsResponse> HandleAsync(
        ListMetricDefinitionsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var revision = await _dbContext.MachineRegistrations
            .AsNoTracking()
            .Where(registration => registration.Id == request.MachineId)
            .Select(registration => registration.MetricSchemaRevision)
            .SingleAsync(cancellationToken);

        var definitions = await _dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.MachineRegistrationId == request.MachineId)
            .OrderBy(metric => metric.CreatedAt)
            .ThenBy(metric => metric.Key)
            .Select(metric => new MetricDefinitionResponse
            {
                Id = metric.Id,
                Key = metric.Key,
                Name = metric.Name,
                Description = metric.Description,
                ValueKind = metric.ValueKind,
                Unit = new MetricUnitResponse
                {
                    QuantityKind = metric.MeasurementUnit.QuantityKind.Name,
                    Name = metric.MeasurementUnit.Name,
                    Abbreviation = metric.MeasurementUnit.Abbreviation,
                },
                Min = metric.Min,
                Max = metric.Max,
                States = metric.States,
                CreatedAt = metric.CreatedAt,
                UpdatedAt = metric.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new ListMetricDefinitionsResponse { Revision = revision, Definitions = definitions };
    }
}
