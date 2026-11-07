using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationListMetricDefinitionsRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.ListMetricDefinitionsRequest;

namespace HealthyFac.Telemetry.Web.MetricDefinitions;

public record ListMetricDefinitionsRequest
{
    [FromRoute(Name = MetricDefinitionRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }
}

public static class ListMetricDefinitionsRequestExtensions
{
    extension(ListMetricDefinitionsRequest request)
    {
        public ApplicationListMetricDefinitionsRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
            };
    }
}
