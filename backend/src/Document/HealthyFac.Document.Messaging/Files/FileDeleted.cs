using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Document.Messaging.Files;

/// <summary>
/// Tells the services that hold data of a file (Insights) that Document deleted the file. A deleted machine sends one
/// message for each of its files.
/// </summary>
public sealed record FileDeleted : IIntegrationMessage, IRequest
{
    public static string MessageType => "document.v1:FileDeleted";

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class FileDeletedValidator : AbstractValidator<FileDeleted>
{
    public FileDeletedValidator()
    {
        RuleFor(message => message.FileId)
            .NotEmpty()
            .WithMessage(FileMessageErrors.Validation.FileIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(FileMessageErrors.Validation.CompanyIdRequired);
    }
}
