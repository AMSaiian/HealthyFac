namespace HealthyFac.Core.Infrastructure.Employees.InviteTokens;

/// <summary>
/// Defines the invite token format, byte lengths, and lifetime limits.
/// </summary>
public static class InviteTokenConstants
{
    public const string KeyIdPattern = "^[A-Za-z0-9-]{1,32}$";

    public static class Format
    {
        public const char Separator = '.';
        public const int PartCount = 3;
        public const string InviteIdFormat = "N";
    }

    public static class Lengths
    {
        public const int SaltBytes = 32;
        public const int MinSecretBytes = 32;
    }

    public static class Lifetime
    {
        public const int DefaultHours = 72;
        public const int MinHours = 1;
        public const int MaxHours = 720;
    }
}
