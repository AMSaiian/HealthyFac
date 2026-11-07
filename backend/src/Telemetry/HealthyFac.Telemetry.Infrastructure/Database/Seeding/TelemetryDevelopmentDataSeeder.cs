using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using HealthyFac.Telemetry.Infrastructure.StatusDefinitions.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Infrastructure.Database.Seeding;

/// <summary>
/// Creates the registrations of the seed machines with their status definitions and the sample metrics at schema
/// revision 1. Core publishes the machines of the
/// same seed data file only after it starts, so the seeder creates the registrations itself. The seeder skips a
/// machine whose registration exists, so a second start adds nothing. The registration is the only trigger, so the
/// later <c>MachineCreated</c> message finds the row and changes nothing.
/// </summary>
/// <param name="dbContext">The telemetry database context.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="timeProvider">Provides the time of the schema revisions.</param>
/// <param name="metricSchemaGenerator">Builds the schemas.</param>
public sealed class TelemetryDevelopmentDataSeeder(
    TelemetryDbContext dbContext,
    IOptions<SeedingOptions> seedingOptions,
    TimeProvider timeProvider,
    IMetricSchemaGenerator metricSchemaGenerator
)
{
    private readonly TelemetryDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IMetricSchemaGenerator _metricSchemaGenerator = metricSchemaGenerator;
    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;

    /// <summary>
    /// Adds the missing registrations of the seed machines and their data in one save.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.TestTenant is not { } tenant)
        {
            return;
        }

        var seeds = _options.GetSeedMachines();
        var seedIds = seeds.Select(seed => seed.MachineId).ToList();
        var registeredIds = await _dbContext.MachineRegistrations
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(registration => seedIds.Contains(registration.Id))
            .Select(registration => registration.Id)
            .ToListAsync(cancellationToken);

        var units = await _dbContext.MeasurementUnits
            .AsNoTracking()
            .Include(unit => unit.QuantityKind)
            .Where(unit => unit.QuantityKind.Name == DiscreteUnits.KindName
                || (unit.QuantityKind.Name == SampleMetricSet.TemperatureQuantityName
                    && unit.Name == SampleMetricSet.TemperatureUnitName))
            .ToListAsync(cancellationToken);
        var temperatureUnit = units.Single(unit => !DiscreteUnits.IsDiscrete(unit));
        var booleanUnit = units.Single(unit => unit.Name == DiscreteUnits.BooleanName);
        var stateUnit = units.Single(unit => unit.Name == DiscreteUnits.StateName);
        var seedOffset = 0;

        foreach (var seed in seeds.Where(seed => !registeredIds.Contains(seed.MachineId)))
        {
            var registration = new MachineRegistration(
                seed.MachineId,
                seed.GatewayId,
                tenant.CompanyId,
                tenant.DepartmentIds[seed.DepartmentIndex]);
            _dbContext.MachineRegistrations.Add(registration);
            _dbContext.MetricSchemaRevisions.Add(
                new MetricSchemaRevision(
                    registration.Id,
                    registration.MetricSchemaRevision,
                    _metricSchemaGenerator.Generate(registration.GatewayId, registration.MetricSchemaRevision, []),
                    _timeProvider.GetUtcNow().UtcDateTime));
            var definitions = SampleMetricSet.CreateDefinitions(
                registration.Id, temperatureUnit, booleanUnit, stateUnit);
            var revision = registration.AdvanceMetricSchemaRevision();
            _dbContext.MetricDefinitions.AddRange(definitions);
            _dbContext.MetricSchemaRevisions.Add(
                new MetricSchemaRevision(
                    registration.Id,
                    revision,
                    _metricSchemaGenerator.Generate(
                        registration.GatewayId,
                        revision,
                        SampleMetricSet.ToSchemaMetrics(definitions, units)),
                    _timeProvider.GetUtcNow().UtcDateTime));
            _dbContext.StatusDefinitions.AddRange(
                new StatusDefinitionFaker(registration, _options.RandomSeed + seedOffset++)
                    .Generate(_options.StatusDefinitionsPerMachine));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
