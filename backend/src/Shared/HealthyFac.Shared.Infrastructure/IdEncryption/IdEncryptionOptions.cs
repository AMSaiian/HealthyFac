using FluentValidation;

namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Configures the identifier encryption keys. Keys are always supplied as an array, even when only one
/// key exists, so adding a second key later needs no configuration reshape.
/// </summary>
public sealed class IdEncryptionOptions
{
    public const string SectionName = "IdEncryption";

    public List<IdEncryptionKeyOptions> Keys { get; init; } = [];
}

public sealed class IdEncryptionOptionsValidator : AbstractValidator<IdEncryptionOptions>
{
    public IdEncryptionOptionsValidator()
    {
        RuleFor(x => x.Keys)
            .NotEmpty()
            .WithMessage(IdEncryptionValidationMessages.Keys.Required)
            .Must(HaveExactlyOneDefaultKey)
            .WithMessage(IdEncryptionValidationMessages.Keys.ExactlyOneDefaultRequired)
            .Must(HaveUniquePrefixes)
            .WithMessage(IdEncryptionValidationMessages.Prefix.DuplicatesNotAllowed);

        RuleForEach(x => x.Keys)
            .ChildRules(key =>
            {
                key.RuleFor(k => k.Prefix)
                    .NotEmpty()
                    .WithMessage(IdEncryptionValidationMessages.Prefix.Required);

                key.RuleFor(k => k.Key)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .WithMessage(IdEncryptionValidationMessages.Key.Required)
                    .Must(BeValidBase64)
                    .WithMessage(IdEncryptionValidationMessages.Key.MustBeBase64)
                    .Must(BeRequiredKeyLength)
                    .WithMessage(IdEncryptionValidationMessages.Key.MustBe256Bits);
            });
    }

    private static bool HaveExactlyOneDefaultKey(List<IdEncryptionKeyOptions> keys) =>
        keys.Count(k => k.IsDefault) == 1;

    private static bool HaveUniquePrefixes(List<IdEncryptionKeyOptions> keys) =>
        keys.Select(k => k.Prefix).Distinct(StringComparer.Ordinal).Count() == keys.Count;

    private static bool BeValidBase64(string key)
    {
        try
        {
            _ = Convert.FromBase64String(key);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool BeRequiredKeyLength(string key)
    {
        try
        {
            return Convert.FromBase64String(key).Length == IdEncryptionConstants.Lengths.RequiredKeyBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
