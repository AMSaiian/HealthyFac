namespace HealthyFac.Shared.Application.IdEncryption;

/// <summary>
/// Provides deterministic authenticated encryption and decryption for identifiers.
/// Uses canonical 16-byte big-endian UUID representation and deterministic AEAD (NIST SP 800-38F / RFC 3394).
/// Formatted as versioned Base64Url strings (e.g., "v1.XYZ...").
/// </summary>
public interface IIdEncryptor
{
    /// <summary>
    /// Encrypts a Guid identifier using the active default encryption version.
    /// </summary>
    /// <param name="id">The plain identifier to encrypt.</param>
    /// <returns>The version-prefixed encrypted identifier token.</returns>
    string Encrypt(Guid id);

    /// <summary>
    /// Decrypts a version-prefixed encrypted identifier token into a Guid.
    /// </summary>
    /// <param name="encryptedId">The version-prefixed encrypted identifier token to decrypt.</param>
    /// <returns>The decrypted identifier.</returns>
    Guid Decrypt(string encryptedId);

    /// <summary>
    /// Re-encrypts an encrypted identifier token using the active default encryption version.
    /// </summary>
    /// <param name="encryptedId">The version-prefixed encrypted identifier token to migrate.</param>
    /// <returns>The re-encrypted identifier token using the active default version.</returns>
    string Migrate(string encryptedId);
}
