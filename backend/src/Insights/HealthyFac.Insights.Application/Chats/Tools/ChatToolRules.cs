using FluentValidation;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Adds the rules that the arguments of several chat tools share.
/// </summary>
public static class ChatToolRules
{
    extension<T>(AbstractValidator<T> validator)
    {
        /// <summary>
        /// Adds the paging rules of an optional limit and an optional offset.
        /// </summary>
        /// <param name="limit">Reads the limit of the request.</param>
        /// <param name="offset">Reads the offset of the request.</param>
        public void AddPagingRules(Func<T, int?> limit, Func<T, int?> offset)
        {
            validator.RuleFor(request => limit(request)!.Value)
                .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
                .When(request => limit(request) is not null)
                .WithMessage(GridErrors.Validation.LimitOutOfRange);

            validator.RuleFor(request => offset(request)!.Value)
                .GreaterThanOrEqualTo(GridValidationConstants.MinOffset)
                .When(request => offset(request) is not null)
                .WithMessage(GridErrors.Validation.OffsetOutOfRange);
        }

        /// <summary>
        /// Adds the rule of a required ID.
        /// </summary>
        /// <param name="id">Reads the ID of the request.</param>
        public void AddIdRule(Func<T, Guid> id) =>
            validator.RuleFor(request => id(request)).NotEmpty().WithMessage(ChatToolErrors.IdRequired);

        /// <summary>
        /// Adds the rule that the start of a range is before its end.
        /// </summary>
        /// <param name="from">Reads the start of the request.</param>
        /// <param name="to">Reads the end of the request.</param>
        public void AddRangeRule(Func<T, DateTime> from, Func<T, DateTime> to) =>
            validator.RuleFor(request => from(request))
                .LessThan(request => to(request))
                .WithMessage(AgentMessages.RangeStartNotBeforeEnd);
    }
}
