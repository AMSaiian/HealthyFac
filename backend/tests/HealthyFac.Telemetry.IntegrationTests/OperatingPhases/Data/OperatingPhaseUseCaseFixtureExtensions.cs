using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;

/// <summary>
/// Runs the phase use cases as a user with a company. Every method opens its own DI scope, so it reads what is
/// stored, not tracked objects.
/// </summary>
public static class OperatingPhaseUseCaseFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<Guid> CreatePhaseUseCaseAsync(CreateOperatingPhaseRequest request)
        {
            using var scope = fixture.CreateUserScope(request.CompanyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateOperatingPhaseRequest, CreateOperatingPhaseResponse>>();

            var response = await handler.HandleAsync(request);

            return response.Id;
        }

        public async Task UpdatePhaseUseCaseAsync(UpdateOperatingPhaseRequest request)
        {
            using var scope = fixture.CreateUserScope(request.CompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateOperatingPhaseRequest>>();

            await handler.HandleAsync(request);
        }

        public async Task DeletePhaseUseCaseAsync(Guid companyId, Guid machineId, Guid phaseId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteOperatingPhaseRequest>>();

            await handler.HandleAsync(
                new DeleteOperatingPhaseRequest { CompanyId = companyId, MachineId = machineId, Id = phaseId });
        }

        public async Task<OperatingPhaseResponse> GetPhaseUseCaseAsync(Guid companyId, Guid machineId, Guid phaseId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetOperatingPhaseRequest, OperatingPhaseResponse>>();

            return await handler.HandleAsync(
                new GetOperatingPhaseRequest { CompanyId = companyId, MachineId = machineId, Id = phaseId });
        }

        public async Task<IReadOnlyList<OperatingPhaseResponse>> ListPhasesUseCaseAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<ListOperatingPhasesRequest, ListOperatingPhasesResponse>>();

            var response = await handler.HandleAsync(
                new ListOperatingPhasesRequest { CompanyId = companyId, MachineId = machineId });

            return response.Items;
        }

        public async Task ReorderPhasesUseCaseAsync(Guid companyId, Guid machineId, IReadOnlyList<Guid> phaseIds)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<ReorderOperatingPhasesRequest>>();

            await handler.HandleAsync(
                new ReorderOperatingPhasesRequest
                    { CompanyId = companyId, MachineId = machineId, PhaseIds = phaseIds });
        }
    }
}
