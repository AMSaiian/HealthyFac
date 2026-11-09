using ApplicationUploadEmbeddedImageRequest =
    HealthyFac.Document.Application.Files.Uploading.UploadEmbeddedImageRequest;

namespace HealthyFac.Document.Web.Files;

public record UploadEmbeddedImageRequest
{
    public IFormFileCollection? Files { get; init; }
}

public static class UploadEmbeddedImageRequestExtensions
{
    extension(UploadEmbeddedImageRequest request)
    {
        public ApplicationUploadEmbeddedImageRequest ToApplicationRequest(Guid companyId) =>
            new() { CompanyId = companyId, File = MultipartFileReader.ReadSingle(request.Files) };
    }
}
