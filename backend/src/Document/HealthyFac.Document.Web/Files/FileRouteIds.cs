using System.Security.Cryptography;
using HealthyFac.Document.Application.Files;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Decrypts the file ID of a route. The machine ID of the route uses <c>MachineRouteIds</c>.
/// </summary>
public static class FileRouteIds
{
    /// <summary>
    /// Decrypts the file ID of a route. A token that is not an encrypted ID names no file, so it gets the same answer
    /// as an unknown file, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The file ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(FileErrors.FileNotFound, exception);
        }
    }
}
