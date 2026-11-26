using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Application.SignalEpisodes;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.SignalEpisodes.Data;

/// <summary>
/// Runs the signal read use cases as a user with a company. Every method opens its own DI scope.
/// </summary>
public static class SignalReadFixtureExtensions
{
    extension(TelemetryEvaluationFixture fixture)
    {
        public async Task<MetricHistoryResponse> GetMetricHistoryUseCaseAsync(GetMetricHistoryRequest request)
        {
            using var scope = fixture.Database.CreateUserScope(request.CompanyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetMetricHistoryRequest, MetricHistoryResponse>>();

            return await handler.HandleAsync(request);
        }

        public async Task<PhaseTimesResponse> GetPhaseTimesUseCaseAsync(GetPhaseTimesRequest request)
        {
            using var scope = fixture.Database.CreateUserScope(request.CompanyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetPhaseTimesRequest, PhaseTimesResponse>>();

            return await handler.HandleAsync(request);
        }

        public async Task SoftDeletePhaseAsync(Guid phaseId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var phase = await dbContext.OperatingPhases.SingleAsync(candidate => candidate.Id == phaseId);

            dbContext.OperatingPhases.Remove(phase);
            await dbContext.SaveChangesAsync();
        }

        public async Task<SignalEpisodesResponse> GetSignalEpisodesUseCaseAsync(GetSignalEpisodesRequest request)
        {
            using var scope = fixture.Database.CreateUserScope(request.CompanyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetSignalEpisodesRequest, SignalEpisodesResponse>>();

            return await handler.HandleAsync(request);
        }
    }
}
