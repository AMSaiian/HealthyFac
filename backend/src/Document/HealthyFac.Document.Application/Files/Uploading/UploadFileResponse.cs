namespace HealthyFac.Document.Application.Files.Uploading;

public record UploadFileResponse
{
    public required Guid Id { get; init; }
}
