using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.MachineErrorCodes;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;

/// <summary>
/// The typed filter of the error code grid. A field without a value adds no condition.
/// </summary>
public sealed record MachineErrorCodeGridFilter : IFilter<MachineErrorCode>
{
    public TextFilter? Code { get; init; }

    public DateRangeFilter? CreatedAt { get; init; }

    public DateRangeFilter? UpdatedAt { get; init; }

    public Expression<Func<MachineErrorCode, bool>>? ToPredicate() =>
        Predicates.From(Code?.ToPredicate<MachineErrorCode>(errorCode => errorCode.Code))
            .And(CreatedAt?.ToPredicate<MachineErrorCode>(errorCode => errorCode.CreatedAt))
            .And(UpdatedAt?.ToPredicate<MachineErrorCode>(errorCode => errorCode.UpdatedAt));
}

public sealed class MachineErrorCodeGridFilterValidator : AbstractValidator<MachineErrorCodeGridFilter>
{
    public MachineErrorCodeGridFilterValidator()
    {
        RuleFor(filter => filter.Code!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Code is not null);

        RuleFor(filter => filter.Code!.Value)
            .MaximumLength(MachineErrorCodeValidationConstants.CodeMaxLength)
            .When(filter => filter.Code?.Value is not null);

        RuleFor(filter => filter.CreatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.CreatedAt is not null);

        RuleFor(filter => filter.UpdatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.UpdatedAt is not null);
    }
}
