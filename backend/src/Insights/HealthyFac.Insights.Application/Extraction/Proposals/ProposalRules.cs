using FluentValidation;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;

namespace HealthyFac.Insights.Application.Extraction.Proposals;

/// <summary>
/// Defines the rules that the extractor tool requests share.
/// </summary>
public static class ProposalRules
{
    extension<T>(IRuleBuilder<T, string?> rule)
    {
        /// <summary>
        /// Checks a description as <see cref="DraftsProposedValidator"/> does: the allow-list and the visible length.
        /// </summary>
        /// <param name="richTextPolicy">Inspects the HTML.</param>
        /// <returns>The rule builder for chaining.</returns>
        public IRuleBuilderOptionsConditions<T, string?> IsProposedDescription(IRichTextPolicy richTextPolicy) =>
            rule.IsRichText(
                richTextPolicy,
                DraftMessageLimits.DescriptionMaxLength,
                DraftMessageErrors.Validation.DescriptionTooLong,
                DraftMessageErrors.Validation.DescriptionNotAllowed,
                DraftMessageErrors.Validation.DescriptionTooMuchFormatting);
    }
}

/// <summary>
/// Defines the status text that a proposal tool returns to the model.
/// </summary>
public static class ProposalToolStatus
{
    public const string Proposed = "proposed";
}
