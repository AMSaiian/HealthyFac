using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Web.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Core.Web.WorkOrders;

public record ListOpenMachineWorkOrdersWebRequest
{
    [FromRoute(Name = WorkOrderRoutes.MachineParameter)]
    public required string MachineId { get; init; }

    [FromQuery(Name = "limit")]
    public required int Limit { get; init; }
}

public static class ListOpenMachineWorkOrdersWebRequestExtensions
{
    extension(ListOpenMachineWorkOrdersWebRequest request)
    {
        public ListOpenMachineWorkOrdersRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(request.MachineId, idEncryptor),
                Limit = request.Limit,
            };
    }
}
