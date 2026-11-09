using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Document.Application.Files.Searching;

/// <summary>
/// The typed filter of the file grids. A field without a value adds no condition.
/// </summary>
public sealed record FileGridFilter : IFilter<FileRef>
{
    public TextFilter? FileName { get; init; }

    public SetFilter<string>? Extension { get; init; }

    /// <summary>The upload time (<c>CreatedAt</c>). <c>From</c> is inclusive and <c>To</c> is exclusive.</summary>
    public DateRangeFilter? UploadedAt { get; init; }

    public Expression<Func<FileRef, bool>>? ToPredicate() =>
        Predicates.From(FileName?.ToPredicate<FileRef>(file => file.FileName))
            .And(Extension?.ToPredicate<FileRef>(file => file.Extension))
            .And(UploadedAt?.ToPredicate<FileRef>(file => file.CreatedAt));
}

public sealed class FileGridFilterValidator : AbstractValidator<FileGridFilter>
{
    public FileGridFilterValidator()
    {
        RuleFor(filter => filter.FileName!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.FileName is not null);

        RuleFor(filter => filter.FileName!.Value)
            .MaximumLength(FileValidationConstants.Text.FileNameMaxLength)
            .When(filter => filter.FileName?.Value is not null);

        RuleFor(filter => filter.Extension!)
            .SetValidator(new TextSetFilterValidator())
            .When(filter => filter.Extension is not null);

        RuleFor(filter => filter.UploadedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.UploadedAt is not null);
    }
}
