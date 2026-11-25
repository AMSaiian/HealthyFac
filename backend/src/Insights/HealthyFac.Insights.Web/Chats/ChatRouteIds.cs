using System.Security.Cryptography;
using HealthyFac.Insights.Application.Chats;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// Decrypts the IDs of a chat route.
/// </summary>
public static class ChatRouteIds
{
    /// <summary>
    /// Decrypts the chat ID of a route. A token that is not an encrypted ID names no chat, so it gets the same answer
    /// as an unknown chat, never a server error.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The chat ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid DecryptChat(string encryptedId, IIdEncryptor idEncryptor) =>
        Decrypt(encryptedId, idEncryptor, ChatErrors.ChatNotFound);

    /// <summary>
    /// Decrypts the message ID of a route. A token that is not an encrypted ID names no message.
    /// </summary>
    /// <param name="encryptedId">The encrypted ID from the route.</param>
    /// <param name="idEncryptor">Decrypts the ID.</param>
    /// <returns>The message ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="encryptedId"/> is not a valid token.</exception>
    public static Guid DecryptMessage(string encryptedId, IIdEncryptor idEncryptor) =>
        Decrypt(encryptedId, idEncryptor, ChatErrors.MessageNotFound);

    private static Guid Decrypt(string encryptedId, IIdEncryptor idEncryptor, string message)
    {
        try
        {
            return idEncryptor.Decrypt(encryptedId);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            throw new NotFoundException(message, exception);
        }
    }
}
