using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Web.MachineRegistrations;
using ApplicationApplyOperatingPhaseDraftRequest =
    HealthyFac.Telemetry.Application.DefinitionDrafts.ApplyOperatingPhaseDraftRequest;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

/// <summary>
/// The body of the apply of a phase draft: the reviewed phase values. The company comes from the token, and the
/// machine and the draft from the route.
/// </summary>
public record ApplyOperatingPhaseDraftRequest : OperatingPhaseInput;

public static class ApplyOperatingPhaseDraftRequestExtensions
{
    extension(ApplyOperatingPhaseDraftRequest request)
    {
        /// <summary>
        /// Builds the Application request.
        /// </summary>
        /// <param name="encryptedMachineId">The encrypted machine ID of the route.</param>
        /// <param name="encryptedDraftId">The encrypted draft ID of the route.</param>
        /// <param name="companyId">The company of the token.</param>
        /// <param name="idEncryptor">Decrypts the IDs.</param>
        /// <returns>The Application request.</returns>
        public ApplicationApplyOperatingPhaseDraftRequest ToApplicationRequest(
            string encryptedMachineId,
            string encryptedDraftId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                MachineId = MachineRouteIds.Decrypt(encryptedMachineId, idEncryptor),
                Id = DefinitionDraftRouteIds.Decrypt(encryptedDraftId, idEncryptor),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                Priority = request.Priority,
                IsTracked = request.IsTracked,
                Conditions = request.Conditions,
                Limits = request.Limits,
            };
    }
}
