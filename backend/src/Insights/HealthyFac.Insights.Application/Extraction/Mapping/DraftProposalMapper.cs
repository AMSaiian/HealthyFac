using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Insights.Application.Extraction.Mapping;

/// <summary>
/// Turns the buffer of the extractor tools into the <see cref="DraftsProposed"/> message. Each tool checked its own
/// proposal, so the mapper only drops a machine without any content (the model proposed it and nothing came after)
/// and runs <see cref="DraftsProposedValidator"/> on the finished message as the last safety net.
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
    /// <returns>The message (or <see langword="null"/> for 0 entries) and the draft count.</returns>
    /// <exception cref="InvalidExtractionException">Thrown when the mapped message fails its validator.</exception>
    public MappingResult Map(ExtractionBuffer buffer, MappingContext context)
    {
        var machines = buffer.Machines.Where(HasContent).ToList();

        if (machines.Count == 0)
        {
            return new MappingResult { DraftCount = 0 };
        }

        var message = new DraftsProposed
        {
            AttemptId = context.AttemptId,
            FileId = context.FileId,
            CompanyId = context.CompanyId,
            IsMachineFile = context.IsMachineFile,
            QueuedAt = context.QueuedAt,
            Machines = machines,
        };

        EnsureValid(message);

        return new MappingResult
        {
            Message = message,
            DraftCount = machines.Count
                + machines.Sum(machine => machine.Metrics.Count + machine.Statuses.Count + machine.Phases.Count),
        };
    }

    private static bool HasContent(ProposedMachine machine) =>
        machine.Name is not null
        || machine.SerialNumber is not null
        || machine.Description is not null
        || machine.Metrics.Count > 0
        || machine.Statuses.Count > 0
        || machine.Phases.Count > 0;

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
