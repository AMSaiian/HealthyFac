using HealthyFac.Shared.Application.IdEncryption;
using ApplicationApplyMachineDraftRequest = HealthyFac.Core.Application.Drafts.ApplyMachineDraftRequest;

namespace HealthyFac.Core.Web.Drafts;

public record ApplyMachineDraftRequest
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public string? Description { get; init; }
    public DateOnly? ExpectedGoLiveOn { get; init; }
    public uint? RowVersion { get; init; }
}

public static class ApplyMachineDraftRequestExtensions
{
    extension(ApplyMachineDraftRequest request)
    {
        public ApplicationApplyMachineDraftRequest ToApplicationRequest(
            string encryptedId,
            Guid companyId,
            IIdEncryptor idEncryptor
        ) =>
            new()
            {
                CompanyId = companyId,
                Id = DraftRouteIds.Decrypt(encryptedId, idEncryptor),
                Name = request.Name,
                SerialNumber = request.SerialNumber,
                Description = request.Description,
                ExpectedGoLiveOn = request.ExpectedGoLiveOn,
                RowVersion = request.RowVersion,
            };
    }
}
