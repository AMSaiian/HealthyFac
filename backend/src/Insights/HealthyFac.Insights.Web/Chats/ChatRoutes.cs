namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// Defines route constants for the chat endpoints.
/// </summary>
public static class ChatRoutes
{
    public const string BaseRoute = "/chats";
    public const string ChatIdParameter = "chatId";
    public const string MessageIdParameter = "messageId";
    public const string LimitParameter = "limit";
    public const string BeforeParameter = "before";
    public const string Search = "/search";
    public const string ById = "/{" + ChatIdParameter + "}";
    public const string Messages = ById + "/messages";
    public const string MessageById = Messages + "/{" + MessageIdParameter + "}";
}
