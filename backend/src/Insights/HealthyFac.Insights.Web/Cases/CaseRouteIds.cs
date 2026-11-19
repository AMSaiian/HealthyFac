using System.Security.Cryptography;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.Web.Cases;

/// <summary>
/// Decrypts the ID of a case route.
/// </summary>
public static class CaseRouteIds
{
    /// <summary>
    /// Decrypts the case ID of a route. A token that is not an encrypted ID names no case, so it gets the same answer
    /// as an unknown case, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The case ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(CaseErrors.CaseNotFound, exception);
        }
    }
}
