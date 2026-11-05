using FluentValidation;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.Application.RichText;

/// <summary>
/// Defines the stateless rule of a rich text field (HTML). Every feature with a rich text field calls it with its own
/// limit and messages, so the rule has one definition.
/// </summary>
public static class RichTextRules
{
    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        /// <summary>
        /// Checks the raw HTML length against <see cref="RichTextConstants.HtmlMaxLength"/> first, so the policy never
        /// parses a huge string. Then it checks the allow-list, and last the visible length. One input gives one
        /// failure. A <see langword="null"/> value passes.
        /// </summary>
        /// <param name="richTextPolicy">Inspects the HTML.</param>
        /// <param name="maxVisibleLength">The longest allowed visible text of the field.</param>
        /// <param name="tooLongMessage">The failure message for visible text above <paramref name="maxVisibleLength"/>.</param>
        /// <param name="notAllowedMessage">The failure message for HTML that the policy rejects.</param>
        /// <param name="tooMuchFormattingMessage">The failure message for HTML above the raw cap.</param>
        /// <returns>The rule builder for chaining.</returns>
        public IRuleBuilderOptionsConditions<T, string?> IsRichText(
            IRichTextPolicy richTextPolicy,
            int maxVisibleLength,
            string tooLongMessage,
            string notAllowedMessage,
            string tooMuchFormattingMessage
        ) =>
            ruleBuilder.MultilineText()
                .Custom((html, context) =>
                {
                    if (html is null)
                    {
                        return;
                    }

                    if (html.Length > RichTextConstants.HtmlMaxLength)
                    {
                        context.AddFailure(tooMuchFormattingMessage);

                        return;
                    }

                    var inspection = richTextPolicy.Inspect(html);

                    if (!inspection.IsAllowed)
                    {
                        context.AddFailure(notAllowedMessage);

                        return;
                    }

                    if (inspection.VisibleLength > maxVisibleLength)
                    {
                        context.AddFailure(tooLongMessage);
                    }
                });
    }
}
