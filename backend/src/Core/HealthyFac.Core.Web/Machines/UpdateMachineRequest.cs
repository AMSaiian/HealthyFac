using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationUpdateMachineRequest = HealthyFac.Core.Application.Machines.UpdateMachineRequest;

namespace HealthyFac.Core.Web.Machines;

public record UpdateMachineRequest
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public string? Description { get; init; }
    public required DateOnly ExpectedGoLiveOn { get; init; }
    public required MachineState State { get; init; }
}

public static class UpdateMachineRequestExtensions
{
    extension(UpdateMachineRequest request)
    {
        public ApplicationUpdateMachineRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = MachineRouteIds.Decrypt(encryptedId, idEncryptor),
                Name = request.Name,
                SerialNumber = request.SerialNumber,
                Description = request.Description,
                ExpectedGoLiveOn = request.ExpectedGoLiveOn,
                State = request.State,
            };
    }
}
