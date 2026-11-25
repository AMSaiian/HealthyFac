namespace HealthyFac.Insights.Application.Chats;

/// <summary>
/// Defines the error messages and the stable keys of the chat use cases.
/// </summary>
public static class ChatErrors
{
    /// <summary>The stable key that the web app maps to a text: the reply to the message still runs.</summary>
    public const string TurnInProgress = "turnInProgress";

    /// <summary>The stable key that the web app maps to a text: the chat takes no new message.</summary>
    public const string ChatBlocked = "chatBlocked";

    public const string CompanyIdRequired = "The company is required.";
    public const string ChatIdRequired = "The chat is required.";
    public const string MessageIdRequired = "The message is required.";
    public const string ChatNotFound = "The chat does not exist.";
    public const string MessageNotFound = "The message does not exist.";
    public const string NotAUserMessage = "Only a message of the user can be deleted.";
    public const string ChatChanged = "The chat changed while the message was deleted. Try again.";
    public const string BeforeInvalid = "The position before which the messages start must be at least 1.";
}
