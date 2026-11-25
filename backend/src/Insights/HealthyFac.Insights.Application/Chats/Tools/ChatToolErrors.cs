namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Defines the texts that a chat tool gives the model. A rejection of another service always gets the same text, so
/// the model never learns whether the target does not exist or the user may not read it.
/// </summary>
public static class ChatToolErrors
{
    public const string NotFoundOrNotAllowed = "not found or not allowed";
    public const string ServiceUnavailable = "This data could not be read; the service is unavailable.";
    public const string IdRequired = "The ID is required.";
    public const string TextHasOneLine = "The text has one line.";
    public const string CaseStateUnknown = "Each state is open, resolved, or closed.";
    public const string SeverityUnknown = "Each severity is warning or alarm.";
    public const string StatesInvalid = "Each state is a text of one line.";
}
