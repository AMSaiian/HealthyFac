using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Insights.Application.Extraction.Mapping;

/// <summary>
/// Turns the buffer of the extractor tools into the <see cref="DraftsProposed"/> message of the source
/// <c>ingest</c>. Each tool checked its own proposal, so the mapper only copies the flat lists of the buffer and runs
/// <see cref="DraftsProposedValidator"/> on the finished message as the last safety net.
/// </summary>
public sealed class DraftProposalMapper
{
    private const string ValidationFailureFormat = "The mapped drafts fail the message rules: {0}";

    private readonly DraftsProposedValidator _validator;

    public DraftProposalMapper(IRichTextPolicy richTextPolicy)
    {
        _validator = new DraftsProposedValidator(richTextPolicy);
    }

    /// <summary>
    /// Maps the buffer.
    /// </summary>
    /// <param name="buffer">The proposals of the run.</param>
    /// <param name="context">The attempt and the file.</param>
    /// <returns>The message (or <see langword="null"/> for 0 drafts) and the draft count.</returns>
    /// <exception cref="InvalidExtractionException">Thrown when the mapped message fails its validator.</exception>
    public MappingResult Map(ExtractionBuffer buffer, MappingContext context)
    {
        var draftCount = buffer.Machines.Count + buffer.Metrics.Count + buffer.Statuses.Count + buffer.Phases.Count;

        if (draftCount == 0)
        {
            return new MappingResult { DraftCount = 0 };
        }

        var message = new DraftsProposed
        {
            Source = ProposalSource.Ingest,
            AttemptId = context.AttemptId,
            FileId = context.FileId,
            CompanyId = context.CompanyId,
            IsMachineFile = context.IsMachineFile,
            QueuedAt = context.QueuedAt,
            Machines = buffer.Machines,
            Metrics = buffer.Metrics,
            Statuses = buffer.Statuses,
            Phases = buffer.Phases,
        };

        EnsureValid(message);

        return new MappingResult { Message = message, DraftCount = draftCount };
    }

    private void EnsureValid(DraftsProposed message)
    {
        var validation = _validator.Validate(message);

        if (!validation.IsValid)
        {
            var errors = string.Join(separator: "; ", validation.Errors.Select(error => error.ErrorMessage).Distinct());

            throw new InvalidExtractionException(string.Format(ValidationFailureFormat, errors));
        }
    }
}
