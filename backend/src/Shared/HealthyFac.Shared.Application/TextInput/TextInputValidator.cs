using FluentValidation;
using FluentValidation.Results;

namespace HealthyFac.Shared.Application.TextInput;

/// <summary>
/// Rejects the strings of every request and message that the database cannot store or that have no meaning in their
/// field: the null character everywhere, control characters in single-line text, and control characters other than
/// <c>\n</c> and <c>\t</c> in multi-line text. <c>AddUseCases</c> registers it as an open generic, so the validation
/// decorator runs it for HTTP requests and for messages. It checks the normalized value: the JSON options normalize
/// on read.
/// </summary>
/// <typeparam name="TRequest">The request or message type.</typeparam>
public sealed class TextInputValidator<TRequest> : AbstractValidator<TRequest>
{
    public override ValidationResult Validate(ValidationContext<TRequest> context) =>
        context.InstanceToValidate is null
            ? new ValidationResult()
            : new ValidationResult(TextInputInspector.Inspect(context.InstanceToValidate));

    public override Task<ValidationResult> ValidateAsync(
        ValidationContext<TRequest> context,
        CancellationToken cancellation = default(CancellationToken)
    ) =>
        Task.FromResult(Validate(context));
}
