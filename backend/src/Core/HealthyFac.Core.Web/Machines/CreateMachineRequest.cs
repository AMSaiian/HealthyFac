namespace HealthyFac.Core.Web.Machines;

public record CreateMachineRequest
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid DepartmentId { get; init; }
}

public static class CreateMachineRequestExtensions
{
    extension(CreateMachineRequest request)
    {
        public Application.Machines.CreateMachine.CreateMachineRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                Name = request.Name,
                SerialNumber = request.SerialNumber,
                CompanyId = companyId,
                DepartmentId = request.DepartmentId,
            };
    }
}
