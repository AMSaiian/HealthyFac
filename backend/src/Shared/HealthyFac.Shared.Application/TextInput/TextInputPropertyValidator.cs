using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Rejects a string that the database cannot store or that has no meaning in its field, with the rules of
/// <see cref="TextInputPolicy.FindViolation"/>. It adds the failure itself and reports the value as valid, so the
/// failure carries no attempted value and no response or log shows the text. A <see langword="null"/> value passes.
/// </summary>
/// <typeparam name="T">The validated object type.</typeparam>
/// <param name="kind">The form of the text.</param>
public sealed class TextInputPropertyValidator<T>(TextInputKind kind) : PropertyValidator<T, string?>
{
    private readonly TextInputKind _kind = kind;

    public override string Name => nameof(TextInputPropertyValidator<T>);

    public override bool IsValid(ValidationContext<T> context, string? value)
    {
        var violation = value is null ? null : TextInputPolicy.FindViolation(value, _kind);
        if (violation is not null)
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, violation));
        }

        return true;
    }
}
