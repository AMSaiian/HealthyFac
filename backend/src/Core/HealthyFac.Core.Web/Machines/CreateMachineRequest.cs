using ApplicationCreateMachineRequest = HealthyFac.Core.Application.Machines.CreateMachineRequest;

namespace HealthyFac.Core.Web.Machines;

public record CreateMachineRequest
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid DepartmentId { get; init; }
    public string? Description { get; init; }
}

public static class CreateMachineRequestExtensions
{
    extension(CreateMachineRequest request)
    {
        public ApplicationCreateMachineRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                Name = request.Name,
                SerialNumber = request.SerialNumber,
                CompanyId = companyId,
                DepartmentId = request.DepartmentId,
                Description = request.Description,
            };
    }
}
