using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.TextInput;

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

    /// <summary>The machines of the files, with an option for files that no machine owns.</summary>
    public FileMachineFilter? Machine { get; init; }

    /// <summary>The ingest states of the files, with an option for files that were never ingested.</summary>
    public FileIngestStateFilter? IngestState { get; init; }

    public Expression<Func<FileRef, bool>>? ToPredicate() =>
        Predicates.From(FileName?.ToPredicate<FileRef>(file => file.FileName))
            .And(Extension?.ToPredicate<FileRef>(file => file.Extension))
            .And(UploadedAt?.ToPredicate<FileRef>(file => file.CreatedAt))
            .And(Machine?.ToPredicate())
            .And(IngestState?.ToPredicate());
}

/// <summary>
/// Matches the files of the given machines, and optionally the files without a machine. The property is named
/// <c>MachineIds</c> so the ID encryption convention decrypts each ID, which a generic set filter cannot do.
/// </summary>
public sealed record FileMachineFilter
{
    public required IReadOnlyList<Guid> MachineIds { get; init; }

    public bool IsUntiedIncluded { get; init; }

    public Expression<Func<FileRef, bool>> ToPredicate()
    {
        var ids = MachineIds;
        var isUntiedIncluded = IsUntiedIncluded;

        return file => (file.MachineId != null && ids.Contains(file.MachineId.Value))
            || (isUntiedIncluded && file.MachineId == null);
    }
}

/// <summary>
/// Matches the files in the given ingest states, and optionally the files that were never ingested.
/// </summary>
public sealed record FileIngestStateFilter
{
    public required IReadOnlyList<string> States { get; init; }

    public bool IsNeverIncluded { get; init; }

    public Expression<Func<FileRef, bool>> ToPredicate()
    {
        var states = States.Select(FileIngestState.FromValue).ToList();
        var isNeverIncluded = IsNeverIncluded;

        return file => (file.IngestState != null && states.Contains(file.IngestState))
            || (isNeverIncluded && file.IngestState == null);
    }
}

public sealed class FileMachineFilterValidator : AbstractValidator<FileMachineFilter>
{
    public FileMachineFilterValidator()
    {
        RuleFor(filter => filter.MachineIds)
            .NotNull()
            .WithMessage(FileErrors.MachineFilterIdsRequired)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage(FileErrors.MachineIdRequired);

        RuleFor(filter => filter)
            .Must(filter => filter.MachineIds.Count > 0 || filter.IsUntiedIncluded)
            .When(filter => filter.MachineIds is not null)
            .WithMessage(FileErrors.MachineFilterEmpty);
    }
}

public sealed class FileIngestStateFilterValidator : AbstractValidator<FileIngestStateFilter>
{
    public FileIngestStateFilterValidator()
    {
        RuleFor(filter => filter.States)
            .NotNull()
            .WithMessage(FileErrors.IngestStateFilterStatesRequired)
            .Must(states => states.All(state => FileIngestState.TryFromValue(state, out _)))
            .WithMessage(FileErrors.IngestStateUnknown);

        RuleForEach(filter => filter.States).SingleLineText();

        RuleFor(filter => filter)
            .Must(filter => filter.States.Count > 0 || filter.IsNeverIncluded)
            .When(filter => filter.States is not null)
            .WithMessage(FileErrors.IngestStateFilterEmpty);
    }
}

public sealed class FileGridFilterValidator : AbstractValidator<FileGridFilter>
{
    public FileGridFilterValidator()
    {
        When(
            filter => filter.FileName is not null,
            () => RuleFor(filter => filter.FileName!).IsTextFilter(FileValidationConstants.Text.FileNameMaxLength));

        When(
            filter => filter.Extension is not null,
            () => RuleFor(filter => filter.Extension!).SetValidator(new TextSetFilterValidator()));

        When(
            filter => filter.UploadedAt is not null,
            () => RuleFor(filter => filter.UploadedAt!).IsDateRangeFilter());

        When(
            filter => filter.Machine is not null,
            () => RuleFor(filter => filter.Machine!).SetValidator(new FileMachineFilterValidator()));

        When(
            filter => filter.IngestState is not null,
            () => RuleFor(filter => filter.IngestState!).SetValidator(new FileIngestStateFilterValidator()));
    }
}
