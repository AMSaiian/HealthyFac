using FluentValidation;

namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

public sealed class InviteTokenOptionsValidator : AbstractValidator<InviteTokenOptions>
{
    public InviteTokenOptionsValidator()
    {
        RuleFor(x => x.LifetimeHours)
            .InclusiveBetween(InviteTokenConstants.Lifetime.MinHours, InviteTokenConstants.Lifetime.MaxHours)
            .WithMessage(InviteTokenValidationMessages.Lifetime.OutOfRange);

        RuleFor(x => x.Keys)
            .NotEmpty()
            .WithMessage(InviteTokenValidationMessages.Keys.Required)
            .Must(HaveExactlyOneDefaultKey)
            .WithMessage(InviteTokenValidationMessages.Keys.ExactlyOneDefaultRequired)
            .Must(HaveUniqueIds)
            .WithMessage(InviteTokenValidationMessages.Keys.DuplicateIdsNotAllowed);

        RuleForEach(x => x.Keys)
            .ChildRules(key =>
            {
                key.RuleFor(k => k.Id)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .WithMessage(InviteTokenValidationMessages.KeyId.Required)
                    .Matches(InviteTokenConstants.KeyIdPattern)
                    .WithMessage(InviteTokenValidationMessages.KeyId.Invalid);

                key.RuleFor(k => k.Secret)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .WithMessage(InviteTokenValidationMessages.Secret.Required)
                    .Must(BeValidBase64)
                    .WithMessage(InviteTokenValidationMessages.Secret.MustBeBase64)
                    .Must(HaveMinimumLength)
                    .WithMessage(InviteTokenValidationMessages.Secret.TooShort);
            });
    }

    private static bool HaveExactlyOneDefaultKey(List<InviteTokenKeyOptions> keys) =>
        keys.Count(k => k.IsDefault) == 1;

    private static bool HaveUniqueIds(List<InviteTokenKeyOptions> keys) =>
        keys.Select(k => k.Id).Distinct(StringComparer.Ordinal).Count() == keys.Count;

    private static bool BeValidBase64(string secret) =>
        Convert.TryFromBase64String(secret, new byte[secret.Length], out _);

    private static bool HaveMinimumLength(string secret) =>
        Convert.TryFromBase64String(secret, new byte[secret.Length], out var byteCount)
        && byteCount >= InviteTokenConstants.Lengths.MinSecretBytes;
}
