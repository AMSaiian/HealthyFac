using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.SignalEpisodes.Data;

/// <summary>
/// Arranges a machine with one metric, readings, and one episode for the signal read tests. Every method opens its
/// own DI scope.
/// </summary>
public static class SignalApiFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public async Task<SignalArrangement> ArrangeSignalMachineAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var registration = new MachineRegistration(
                Guid.CreateVersion7(),
                GatewayIds.Create(),
                fixture.CompanyId,
                new DateOnly(year: 2020, month: 1, day: 1),
                new DateTime(year: 2019, month: 12, day: 31, hour: 22, minute: 0, second: 0, DateTimeKind.Utc));
            var unit = await dbContext.MeasurementUnits
                .Include(candidate => candidate.QuantityKind)
                .Where(candidate => candidate.QuantityKind.Name == "Temperature" && candidate.Name == "DegreeCelsius")
                .SingleAsync();
            var metric = new MetricDefinition(
                registration.Id,
                SignalApiTestData.MetricKey,
                name: "Spindle temperature",
                description: null,
                MetricValueKind.Numeric,
                unit,
                min: -20,
                max: 200,
                states: null);

            dbContext.MachineRegistrations.Add(registration);
            dbContext.MetricDefinitions.Add(metric);
            await dbContext.SaveChangesAsync();

            foreach (var (second, value) in SignalApiTestData.Readings)
            {
                dbContext.NumericReadings.Add(
                    new NumericReading(
                        metric.Id,
                        SignalApiTestData.RangeStart.AddSeconds(second),
                        value,
                        isOutOfRange: false));
            }

            dbContext.SignalEpisodes.Add(
                SignalEpisode.Open(
                    registration.Id,
                    fixture.CompanyId,
                    SignalRule.Threshold,
                    metric.Id,
                    statusDefinitionId: null,
                    SignalSeverity.Warning,
                    SignalApiTestData.RangeStart.AddSeconds(value: 10),
                    operatingPhaseId: null,
                    new SignalSnapshot { Value = 91, Limit = 90, PhaseCode = SignalApiTestData.PhaseCode }));
            await dbContext.SaveChangesAsync();

            return new SignalArrangement(registration.Id, metric.Id);
        }

        public string MetricHistoryPath(SignalArrangement arrangement, int bucketCount) =>
            $"{SignalApiTestData.MachinesPath}/{fixture.IdEncryptor.Encrypt(arrangement.MachineId)}/metric-history"
            + $"?metricDefinitionId={Uri.EscapeDataString(fixture.IdEncryptor.Encrypt(arrangement.MetricId))}"
            + $"&from={SignalApiTestData.RangeStartText}&to={SignalApiTestData.RangeEndText}"
            + $"&bucketCount={bucketCount}";

        public string SignalEpisodesPath(SignalArrangement arrangement) =>
            $"{SignalApiTestData.MachinesPath}/{fixture.IdEncryptor.Encrypt(arrangement.MachineId)}/signal-episodes"
            + $"?from={SignalApiTestData.RangeStartText}&to={SignalApiTestData.RangeEndText}";
    }
}

public sealed record SignalArrangement(Guid MachineId, Guid MetricId);
