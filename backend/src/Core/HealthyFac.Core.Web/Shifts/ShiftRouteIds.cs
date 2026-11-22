using System.Security.Cryptography;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Core.Web.Shifts;

/// <summary>
/// Decrypts the IDs of a shift route.
/// </summary>
public static class ShiftRouteIds
{
    /// <summary>
    /// Decrypts an ID of a route. A token that is not an encrypted ID names nothing, so it gets the same answer as an
    /// unknown resource, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(ShiftErrors.Rejections.EmployeeNotFound, exception);
        }
    }

    /// <summary>
    /// Decrypts an optional ID of a query. A missing, malformed, or foreign token names nothing, so it is ignored.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the query, or <see langword="null"/>.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The ID, or <see langword="null"/> when the token is missing or not valid.</returns>
    public static Guid? TryDecrypt(string? encryptedId, IIdEncryptor idEncryptor)
    {
        if (string.IsNullOrEmpty(encryptedId))
        {
            return null;
        }

        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            return null;
        }
    }
}
