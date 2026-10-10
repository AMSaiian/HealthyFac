namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Defines the validator messages for identifier encryption configuration, grouped by the setting they report.
/// These are strings because FluentValidation reports text, not exceptions.
/// </summary>
public static class IdEncryptionValidationMessages
{
    public static class Keys
    {
        public const string Required = "At least one IdEncryption key must be configured.";
        public const string ExactlyOneDefaultRequired = "Exactly one encryption key must be marked as default.";
    }

    public static class Key
    {
        public const string Required = "IdEncryption Key configuration is required.";
        public const string MustBeBase64 = "IdEncryption Key configuration must be a valid Base64 string.";

        public const string MustBe256Bits =
            "IdEncryption Key configuration must decode to exactly 32 bytes (256-bit).";
    }

    public static class Prefix
    {
        public const string Required = "Encryption key version prefix is required.";
        public const string DuplicatesNotAllowed = "Duplicate encryption key version prefixes are not allowed.";
    }
}
