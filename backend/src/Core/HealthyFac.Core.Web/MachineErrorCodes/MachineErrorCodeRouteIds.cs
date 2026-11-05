using System.Security.Cryptography;
using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Core.Web.MachineErrorCodes;

/// <summary>
/// Decrypts the error code ID of a route. The machine ID of the route uses <c>MachineRouteIds</c>.
/// </summary>
public static class MachineErrorCodeRouteIds
{
    /// <summary>
    /// Decrypts the error code ID of a route. A token that is not an encrypted ID names no error code, so it gets
    /// the same answer as an unknown error code, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The error code ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(MachineErrorCodeErrors.ErrorCodeNotFound, exception);
        }
    }
}
