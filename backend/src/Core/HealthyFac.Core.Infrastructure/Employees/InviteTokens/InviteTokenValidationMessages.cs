namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

/// <summary>
/// Defines the validator messages for invite token configuration, grouped by the setting they report.
/// These are strings because FluentValidation reports text, not exceptions.
/// </summary>
public static class InviteTokenValidationMessages
{
    public static class Keys
    {
        public const string Required = "At least one InviteTokens key must be configured.";
        public const string ExactlyOneDefaultRequired = "Exactly one invite token key must be marked as default.";
        public const string DuplicateIdsNotAllowed = "Duplicate invite token key IDs are not allowed.";
    }

    public static class KeyId
    {
        public const string Required = "Invite token key ID is required.";
        public const string Invalid = "Invite token key ID must have 1 to 32 letters, digits, or hyphens.";
    }

    public static class Secret
    {
        public const string Required = "Invite token key secret is required.";
        public const string MustBeBase64 = "Invite token key secret must be a valid Base64 string.";
        public const string TooShort = "Invite token key secret must decode to at least 32 bytes.";
    }

    public static class Lifetime
    {
        public const string OutOfRange = "InviteTokens LifetimeHours must be from 1 to 720.";
    }
}
