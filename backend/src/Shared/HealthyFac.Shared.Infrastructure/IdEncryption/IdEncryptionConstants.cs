namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Defines version prefixes, byte lengths, and property naming conventions for identifier encryption.
/// </summary>
public static class IdEncryptionConstants
{
    public const string CurrentVersionPrefix = "v1.";

    public static class Lengths
    {
        public const int RequiredKeyBytes = 32;
        public const int PlaintextGuidBytes = 16;
        public const int ExpectedCiphertextBytes = 24;
    }

    public static class Conventions
    {
        public const string PropertySuffix = "Id";
        public const string CollectionPropertySuffix = "Ids";
        public const string ExactPropertyName = "id";
    }
}
