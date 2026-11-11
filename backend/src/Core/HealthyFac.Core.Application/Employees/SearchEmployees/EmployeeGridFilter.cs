using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Core.Application.Employees.SearchEmployees;

/// <summary>
/// The typed filter of the employee grid. A field without a value adds no condition.
/// </summary>
public sealed record EmployeeGridFilter : IFilter<Employee>
{
    public TextFilter? Name { get; init; }

    public TextFilter? Email { get; init; }

    public SetFilter<RoleCode>? Roles { get; init; }

    public SetFilter<EmployeeStatus>? Statuses { get; init; }

    public Expression<Func<Employee, bool>>? ToPredicate() =>
        Predicates.From(Name?.ToPredicate<Employee>(employee => employee.FirstName + " " + employee.LastName))
            .And(Email?.ToPredicate<Employee>(employee => employee.Email))
            .And(Roles?.ToPredicate<Employee>(employee => employee.Role.Code))
            .And(Statuses?.ToPredicate<Employee>(employee => employee.Status));
}

public sealed class EmployeeGridFilterValidator : AbstractValidator<EmployeeGridFilter>
{
    public EmployeeGridFilterValidator()
    {
        RuleFor(filter => filter.Name!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Name is not null);

        RuleFor(filter => filter.Name!.Value)
            .MaximumLength(EmployeeValidationConstants.NameFilterMaxLength)
            .When(filter => filter.Name?.Value is not null);

        RuleFor(filter => filter.Email!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Email is not null);

        RuleFor(filter => filter.Email!.Value)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength)
            .When(filter => filter.Email?.Value is not null);

        RuleFor(filter => filter.Roles!)
            .SetValidator(new SetFilterValidator<RoleCode>())
            .When(filter => filter.Roles is not null);

        RuleFor(filter => filter.Statuses!)
            .SetValidator(new SetFilterValidator<EmployeeStatus>())
            .When(filter => filter.Statuses is not null);
    }
}
