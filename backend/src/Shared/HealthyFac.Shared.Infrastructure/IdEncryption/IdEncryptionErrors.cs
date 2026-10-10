using System.Security.Cryptography;

namespace HealthyFac.Shared.Infrastructure.IdEncryption;

/// <summary>
/// Defines the exceptions that identifier encryption throws.
/// Validator messages live in <see cref="IdEncryptionValidationMessages"/>.
/// </summary>
public static class IdEncryptionErrors
{
    public static ArgumentException InvalidKeyLength(int actualLength) =>
        new(
            $"Key size must be exactly {IdEncryptionConstants.Lengths.RequiredKeyBytes} bytes (256-bit). "
            + $"Provided: {actualLength} bytes.",
            nameof(actualLength)
        );

    public static FormatException UnsupportedVersionPrefix(string encryptedId) =>
        new($"Unsupported or missing ID encryption version prefix in '{encryptedId}'.");

    public static FormatException InvalidBase64UrlEncoding(Exception innerException) =>
        new("Invalid Base64Url encoding in encrypted identifier.", innerException);

    public static FormatException UnexpectedCiphertextLength(int actualLength) =>
        new(
            $"Unexpected ciphertext payload length {actualLength} bytes. "
            + $"Expected: {IdEncryptionConstants.Lengths.ExpectedCiphertextBytes} bytes."
        );

    public static CryptographicException AuthenticationFailed(Exception innerException) =>
        new("Identifier authentication failed: integrity check tag is invalid or tampered.", innerException);

    public static CryptographicException PayloadLengthMismatch() =>
        new("Decrypted payload length mismatch.");

    public static InvalidOperationException NoKeysConfigured(string configurationPath) =>
        new(
            $"Missing required configuration '{configurationPath}:Keys'. "
            + $"At least one {IdEncryptionConstants.Lengths.RequiredKeyBytes * 8}-bit key is required."
        );

    public static InvalidOperationException NoDefaultKeyConfigured() =>
        new("No default ID encryption key configuration was found.");
}
