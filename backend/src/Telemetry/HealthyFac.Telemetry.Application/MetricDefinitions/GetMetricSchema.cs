using System.Text.Json;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

public record GetMetricSchemaRequest : IRequest<GetMetricSchemaResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

/// <summary>
/// The payload schema of the last applied revision. The schema is a JSON value, not a string.
/// </summary>
public record GetMetricSchemaResponse
{
    public required int Revision { get; init; }

    public required JsonElement Schema { get; init; }
}

public class GetMetricSchemaRequestValidator : AbstractValidator<GetMetricSchemaRequest>
{
    public GetMetricSchemaRequestValidator()
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
public sealed class GetMetricSchemaPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMetricSchemaRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetMetricSchemaRequest request, CancellationToken cancellationToken)
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
/// Reads the schema with the highest revision number of a machine. Every machine has one from its registration on.
/// </summary>
/// <param name="dbContext">Reads the schema revisions.</param>
public sealed class GetMetricSchemaRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetMetricSchemaRequest, GetMetricSchemaResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<GetMetricSchemaResponse> HandleAsync(
        GetMetricSchemaRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var latest = await _dbContext.MetricSchemaRevisions
            .AsNoTracking()
            .Where(revision => revision.MachineRegistrationId == request.MachineId)
            .OrderByDescending(revision => revision.Revision)
            .Select(revision => new { revision.Revision, revision.Schema })
            .FirstAsync(cancellationToken);

        using var document = JsonDocument.Parse(latest.Schema);

        return new GetMetricSchemaResponse { Revision = latest.Revision, Schema = document.RootElement.Clone() };
    }
}
