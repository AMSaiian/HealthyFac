using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Core.Application.Employees.SearchEmployeeInvites;

/// <summary>
/// The typed filter of the pending invite grid. A field without a value adds no condition.
/// </summary>
public sealed record EmployeeInviteGridFilter : IFilter<EmployeeInvite>
{
    public TextFilter? Email { get; init; }

    public Expression<Func<EmployeeInvite, bool>>? ToPredicate() =>
        Predicates.From(Email?.ToPredicate<EmployeeInvite>(invite => invite.Email));
}

public sealed class EmployeeInviteGridFilterValidator : AbstractValidator<EmployeeInviteGridFilter>
{
    public EmployeeInviteGridFilterValidator()
    {
        RuleFor(filter => filter.Email!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Email is not null);

        RuleFor(filter => filter.Email!.Value)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength)
            .When(filter => filter.Email?.Value is not null);
    }
}
