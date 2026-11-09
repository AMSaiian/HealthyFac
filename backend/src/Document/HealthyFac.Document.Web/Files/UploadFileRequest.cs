using ApplicationUploadFileRequest = HealthyFac.Document.Application.Files.Uploading.UploadFileRequest;

namespace HealthyFac.Document.Web.Files;

public record UploadFileRequest
{
    public IFormFileCollection? Files { get; init; }

    public string? Description { get; init; }
}

public static class UploadFileRequestExtensions
{
    private const string DescriptionKey = "description";

    extension(UploadFileRequest request)
    {
        public ApplicationUploadFileRequest ToApplicationRequest(Guid companyId, Guid? machineId) =>
            new()
            {
                CompanyId = companyId,
                MachineId = machineId,
                File = MultipartFileReader.ReadSingle(request.Files),
                Description = MultipartText.Normalize(request.Description, DescriptionKey),
            };
    }
}
