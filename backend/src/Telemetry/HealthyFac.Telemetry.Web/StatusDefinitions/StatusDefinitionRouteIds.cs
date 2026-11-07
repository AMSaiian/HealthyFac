using System.Security.Cryptography;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.StatusDefinitions;

namespace HealthyFac.Telemetry.Web.StatusDefinitions;

/// <summary>
/// Decrypts the status definition ID of a route. The machine ID of the route uses <c>MachineRouteIds</c>.
/// </summary>
public static class StatusDefinitionRouteIds
{
    /// <summary>
    /// Decrypts the status definition ID of a route. A token that is not an encrypted ID names no status definition, so
    /// it gets the same answer as an unknown status definition, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The status definition ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(StatusDefinitionErrors.StatusDefinitionNotFound, exception);
        }
    }
}
