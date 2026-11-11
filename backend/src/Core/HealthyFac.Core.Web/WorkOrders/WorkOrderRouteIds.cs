using System.Security.Cryptography;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Core.Web.WorkOrders;

/// <summary>
/// Decrypts the work order ID of a route.
/// </summary>
public static class WorkOrderRouteIds
{
    /// <summary>
    /// Decrypts the work order ID of a route. A token that is not an encrypted ID names no work order, so it gets the
    /// same answer as an unknown work order, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The work order ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound, exception);
        }
    }
}
