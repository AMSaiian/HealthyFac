using System.Security.Cryptography;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Core.Web.Employees;

/// <summary>
/// Decrypts the invite ID of a route.
/// </summary>
public static class EmployeeInviteRouteIds
{
    /// <summary>
    /// Decrypts the invite ID of a route. A token that is not an encrypted ID names no invite, so it gets the same
    /// answer as an unknown invite, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The invite ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteNotFound, exception);
        }
    }
}
