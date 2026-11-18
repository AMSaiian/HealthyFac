using System.Security.Cryptography;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Telemetry.Application.DefinitionDrafts;

namespace HealthyFac.Telemetry.Web.DefinitionDrafts;

/// <summary>
/// Decrypts the draft and attempt IDs of a route. The machine ID of the route uses <c>MachineRouteIds</c>.
/// </summary>
public static class DefinitionDraftRouteIds
{
    /// <summary>
    /// Decrypts an ID of a route. A token that is not an encrypted ID names no draft, so it gets the same answer as
    /// an unknown draft, never a server error.
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
            throw new NotFoundException(DefinitionDraftErrors.DraftNotFound, exception);
        }
    }
}
