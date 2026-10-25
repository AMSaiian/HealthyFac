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
    private const string WhitespaceKey = "   ";

    public const string DefaultPrefix = "v1.";

    public static TheoryData<string> ValidKeys => new()
    {
        Valid256BitKey,
    };

    public static TheoryData<string, string> InvalidKeysWithExpectedError => new()
    {
        { null!, IdEncryptionValidationMessages.Key.Required },
        { string.Empty, IdEncryptionValidationMessages.Key.Required },
        { WhitespaceKey, IdEncryptionValidationMessages.Key.Required },
        { NotBase64Key, IdEncryptionValidationMessages.Key.MustBeBase64 },
        { Short128BitKey, IdEncryptionValidationMessages.Key.MustBe256Bits },
        { Long512BitKey, IdEncryptionValidationMessages.Key.MustBe256Bits },
    };
}
