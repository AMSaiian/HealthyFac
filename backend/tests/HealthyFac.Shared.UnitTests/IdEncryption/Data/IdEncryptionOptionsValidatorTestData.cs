using HealthyFac.Shared.Infrastructure.IdEncryption;

namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public static class IdEncryptionOptionsValidatorTestData
{
    private static readonly string Valid256BitKey = Convert.ToBase64String(
        new byte[IdEncryptionConstants.Lengths.RequiredKeyBytes]
    );

    private static readonly string Short128BitKey = Convert.ToBase64String(new byte[16]);
    private static readonly string Long512BitKey = Convert.ToBase64String(new byte[64]);
    private const string NotBase64Key = "not-a-valid-base64-string!@#$";

    public const string DefaultPrefix = "v1.";
    public const string SecondPrefix = "v2.";

    public static TheoryData<string> ValidKeys => new()
    {
        Valid256BitKey,
    };

    public static TheoryData<string, string> InvalidKeysWithExpectedError => new()
    {
        { NotBase64Key, IdEncryptionValidationMessages.Key.MustBeBase64 },
        { Short128BitKey, IdEncryptionValidationMessages.Key.MustBe256Bits },
        { Long512BitKey, IdEncryptionValidationMessages.Key.MustBe256Bits },
    };

    public static TheoryData<IdEncryptionOptions, string> InvalidKeySetsWithExpectedError => new()
    {
        {
            CreateOptions(CreateKey(DefaultPrefix, isDefault: false), CreateKey(SecondPrefix, isDefault: false)),
            IdEncryptionValidationMessages.Keys.ExactlyOneDefaultRequired
        },
        {
            CreateOptions(CreateKey(DefaultPrefix, isDefault: true), CreateKey(SecondPrefix, isDefault: true)),
            IdEncryptionValidationMessages.Keys.ExactlyOneDefaultRequired
        },
        {
            CreateOptions(CreateKey(DefaultPrefix, isDefault: true), CreateKey(DefaultPrefix, isDefault: false)),
            IdEncryptionValidationMessages.Prefix.DuplicatesNotAllowed
        },
    };

    private static IdEncryptionOptions CreateOptions(params IdEncryptionKeyOptions[] keys) =>
        new() { Keys = [.. keys] };

    private static IdEncryptionKeyOptions CreateKey(string prefix, bool isDefault) =>
        new()
        {
            Prefix = prefix,
            Key = Valid256BitKey,
            IsDefault = isDefault,
        };
}
