using System.Runtime.CompilerServices;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.Domain.Chats;

/// <summary>
/// Defines the exceptions that a chat throws when a caller breaks its rules. The Application layer checks each rule
/// first and answers with its own status, so these exceptions mark a programming error.
/// </summary>
public static class ChatInvariantErrors
{
    public static InvalidOperationException ChatBlocked(Guid chatId) =>
        new($"Chat '{chatId}' is blocked, so it takes no new message.");

    public static InvalidOperationException NotAUserMessage(Guid chatId, Guid messageId) =>
        new($"Message '{messageId}' of chat '{chatId}' is not a user message.");

    public static InvalidOperationException MessageNotFound(Guid chatId, Guid messageId) =>
        new($"Chat '{chatId}' has no message '{messageId}'.");

    public static InvalidOperationException TurnInProgress(Guid chatId, Guid messageId) =>
        new($"The reply to message '{messageId}' of chat '{chatId}' is still running.");

    public static InvalidOperationException IllegalTransition(
        Guid messageId,
        ChatMessageState from,
        [CallerMemberName] string action = ""
    ) =>
        new($"Message '{messageId}' is {from.Value}, so it cannot {action}.");
}
