using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetMetricSchemaRequest =
    HealthyFac.Telemetry.Application.MetricDefinitions.GetMetricSchemaRequest;

namespace HealthyFac.Telemetry.Web.MetricDefinitions;

public record GetMetricSchemaRequest
{
    [FromRoute(Name = MetricDefinitionRoutes.MachineIdParameter)]
    public required string MachineId { get; init; }
}

public static class GetMetricSchemaRequestExtensions
{
    extension(GetMetricSchemaRequest request)
    {
        public ApplicationGetMetricSchemaRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
            };
    }
}
