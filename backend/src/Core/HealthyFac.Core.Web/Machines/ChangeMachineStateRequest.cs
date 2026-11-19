using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.IdEncryption;
using ApplicationChangeMachineStateRequest = HealthyFac.Core.Application.Machines.States.ChangeMachineStateRequest;

namespace HealthyFac.Core.Web.Machines;

public record ChangeMachineStateRequest
{
    public required MachineState State { get; init; }
}

public static class ChangeMachineStateRequestExtensions
{
    extension(ChangeMachineStateRequest request)
    {
        public ApplicationChangeMachineStateRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = MachineRouteIds.Decrypt(encryptedId, idEncryptor),
                State = request.State,
            };
    }
}
