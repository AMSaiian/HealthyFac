using System.Security.Cryptography;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.Web.Ingest;

/// <summary>
/// Decrypts the IDs of an ingest route.
/// </summary>
public static class IngestRouteIds
{
    /// <summary>
    /// Decrypts the file ID of a route. A token that is not an encrypted ID names no file, so it gets the same answer
    /// as an unknown file, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The file ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid DecryptFile(string encryptedId, IIdEncryptor idEncryptor) =>
        Decrypt(encryptedId, idEncryptor, IngestErrors.FileNotFound);

    /// <summary>
    /// Decrypts the attempt ID of a route. A token that is not an encrypted ID names no attempt, so it gets the same
    /// answer as an unknown attempt, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The attempt ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid DecryptAttempt(string encryptedId, IIdEncryptor idEncryptor) =>
        Decrypt(encryptedId, idEncryptor, IngestErrors.AttemptNotFound);

    private static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor, string notFoundMessage)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(notFoundMessage, exception);
        }
    }
}
