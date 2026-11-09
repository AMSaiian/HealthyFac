using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Document.Application.Files.Uploading;

/// <summary>
/// The file part of an upload as plain data, so the Application layer needs no web type.
/// </summary>
public sealed class FileUpload
{
    /// <summary>The content. The stream is seekable, because the inspector reads it twice.</summary>
    public required Stream Content { get; init; }

    /// <summary>The file name as the client sent it. It may hold a path.</summary>
    public required string ClientFileName { get; init; }

    /// <summary>The media type that the client declared.</summary>
    public required string DeclaredContentType { get; init; }

    /// <summary>The length of the content in bytes.</summary>
    public required long Length { get; init; }
}

/// <summary>
/// Validates the parts of an upload that do not depend on the purpose. The size limit depends on the purpose, so each
/// request validator checks it.
/// </summary>
public sealed class FileUploadValidator : AbstractValidator<FileUpload>
{
    private const int DeclaredTypeMaxLength = 255;

    public FileUploadValidator()
    {
        RuleFor(upload => upload.ClientFileName)
            .Cascade(CascadeMode.Stop)
            .SingleLineText()
            .Must(name => name is not null && FileNames.ToBaseName(name) is not null)
            .WithMessage(FileErrors.NameRequired)
            .Must(name => FileNames.ToBaseName(name!)!.Length <= FileValidationConstants.Text.FileNameMaxLength)
            .WithMessage(FileErrors.NameTooLong);

        RuleFor(upload => upload.DeclaredContentType)
            .MaximumLength(DeclaredTypeMaxLength)
            .WithMessage(FileErrors.DeclaredTypeTooLong)
            .SingleLineText();

        RuleFor(upload => upload.Length)
            .GreaterThan(valueToCompare: 0)
            .WithMessage(FileErrors.FileEmpty);
    }
}
