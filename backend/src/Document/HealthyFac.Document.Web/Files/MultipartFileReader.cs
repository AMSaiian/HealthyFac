using FluentValidation.Results;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Uploading;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Reads the file part of a multipart upload.
/// </summary>
public static class MultipartFileReader
{
    private const string FileKey = "file";

    /// <summary>
    /// Takes the one file part of the form and normalizes its name and type. The framework has buffered the form: a
    /// part above the memory threshold is on disk, and its stream is seekable.
    /// </summary>
    /// <param name="files">The file parts of the form, or <see langword="null"/>.</param>
    /// <returns>The uploaded file as plain data.</returns>
    /// <exception cref="AppValidationException">Thrown when the form holds no file part or more than one.</exception>
    public static FileUpload ReadSingle(IFormFileCollection? files)
    {
        if (files is not { Count: 1 })
        {
            throw new AppValidationException([new ValidationFailure(FileKey, FileErrors.ExactlyOneFile)]);
        }

        var file = files[index: 0];

        return new FileUpload
        {
            Content = file.OpenReadStream(),
            ClientFileName = MultipartText.Normalize(file.FileName, FileKey)!,
            DeclaredContentType = MultipartText.Normalize(file.ContentType, FileKey)!,
            Length = file.Length,
        };
    }
}
