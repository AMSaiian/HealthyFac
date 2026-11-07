using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;

/// <summary>
/// The typed filter of the status definition grid. A field without a value adds no condition.
/// </summary>
public sealed record StatusDefinitionGridFilter : IFilter<StatusDefinition>
{
    public TextFilter? Code { get; init; }

    public SetFilter<StatusSeverity>? Severities { get; init; }

    public DateRangeFilter? CreatedAt { get; init; }

    public DateRangeFilter? UpdatedAt { get; init; }

    public Expression<Func<StatusDefinition, bool>>? ToPredicate() =>
        Predicates.From(Code?.ToPredicate<StatusDefinition>(status => status.Code))
            .And(Severities?.ToPredicate<StatusDefinition>(status => status.Severity))
            .And(CreatedAt?.ToPredicate<StatusDefinition>(status => status.CreatedAt))
            .And(UpdatedAt?.ToPredicate<StatusDefinition>(status => status.UpdatedAt));
}

public sealed class StatusDefinitionGridFilterValidator : AbstractValidator<StatusDefinitionGridFilter>
{
    public StatusDefinitionGridFilterValidator()
    {
        RuleFor(filter => filter.Code!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Code is not null);

        RuleFor(filter => filter.Code!.Value)
            .MaximumLength(StatusDefinitionValidationConstants.CodeMaxLength)
            .When(filter => filter.Code?.Value is not null);

        RuleFor(filter => filter.Severities!)
            .SetValidator(new SetFilterValidator<StatusSeverity>())
            .When(filter => filter.Severities is not null);

        RuleFor(filter => filter.CreatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.CreatedAt is not null);

        RuleFor(filter => filter.UpdatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.UpdatedAt is not null);
    }
}
