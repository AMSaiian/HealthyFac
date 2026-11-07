using EntityFramework.Exceptions.Common;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MachineRegistrations;

public sealed class RegisterMachinePreHandlerCheck : PassThroughPreHandlerCheck<MachineCreated>;

/// <summary>
/// Creates the registration of a machine that Core created. When the registration already exists (for example from
/// the development seed, or after a deleted machine), the handler changes nothing and succeeds.
/// </summary>
/// <remarks>
/// The handler follows the form <b>Resume</b> (rule <b>Cancellation Tokens</b>): it finds the registration first, and
/// creates it only when it is missing. A parallel create of the same machine ends in the unique index of the ID. The
/// handler then reads the registration again and succeeds, because the other writer made the same row.
/// </remarks>
/// <param name="dbContext">Reads and saves the registration.</param>
/// <param name="timeProvider">Provides the time of the first schema revision.</param>
/// <param name="metricSchemaGenerator">Builds the first schema.</param>
public sealed class RegisterMachineRequestHandler(
    ITelemetryDbContext dbContext,
    TimeProvider timeProvider,
    IMetricSchemaGenerator metricSchemaGenerator
) : RequestHandler<MachineCreated>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IMetricSchemaGenerator _metricSchemaGenerator = metricSchemaGenerator;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(MachineCreated request, CancellationToken cancellationToken)
    {
        var isRegistered = await IsRegisteredAsync(request.MachineId, cancellationToken);

        if (isRegistered)
        {
            return;
        }

        var registration = new MachineRegistration(
            request.MachineId, request.GatewayId, request.CompanyId, request.DepartmentId);
        _dbContext.MachineRegistrations.Add(registration);
        _dbContext.MetricSchemaRevisions.Add(
            new MetricSchemaRevision(
                registration.Id,
                registration.MetricSchemaRevision,
                _metricSchemaGenerator.Generate(registration.GatewayId, registration.MetricSchemaRevision, []),
                _timeProvider.GetUtcNow().UtcDateTime));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            var isRegisteredByOther = await IsRegisteredAsync(request.MachineId, cancellationToken);

            if (!isRegisteredByOther)
            {
                throw;
            }
        }
    }

    private Task<bool> IsRegisteredAsync(Guid machineId, CancellationToken cancellationToken) =>
        _dbContext.MachineRegistrations
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .AnyAsync(registration => registration.Id == machineId, cancellationToken);
}
