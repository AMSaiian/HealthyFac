using System.Security.Cryptography;
using HealthyFac.Core.Application.WorkOrders.Drafts;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Core.Web.WorkOrders.Drafts;

/// <summary>
/// Decrypts the work order draft ID of a route.
/// </summary>
public static class WorkOrderDraftRouteIds
{
    /// <summary>
    /// Decrypts the draft ID of a route. A token that is not an encrypted ID names no draft, so it gets the same
    /// answer as an unknown draft, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The draft ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(WorkOrderDraftErrors.DraftNotFound, exception);
        }
    }
}
