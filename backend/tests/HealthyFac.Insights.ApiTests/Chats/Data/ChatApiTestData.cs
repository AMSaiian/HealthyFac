namespace HealthyFac.Insights.ApiTests.Chats.Data;

public static class ChatApiTestData
{
    public const string ReadPermission = "chats:read";
    public const string WritePermission = "chats:write";
    public const string BasePath = "/v1/chats";
    public const string SearchPath = BasePath + "/search";
    public const string SearchBody = "{\"limit\":20}";
    public const string ChatIdToken = "{chatId}";
    public const string MessageIdToken = "{messageId}";
    public const string MalformedId = "not-an-encrypted-id";
    public const string Question = "Why is the spindle hot?";
    public const string Answer = "Check the filter.";

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 25, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    public static string ByIdPath(string encryptedId) => $"{BasePath}/{encryptedId}";

    public static string MessagesPath(string encryptedId) => $"{BasePath}/{encryptedId}/messages?limit=10";

    public static string MessageByIdPath(string encryptedChatId, string encryptedMessageId) =>
        $"{BasePath}/{encryptedChatId}/messages/{encryptedMessageId}";

    /// <summary>
    /// The endpoints that name a chat: the case name, the method, the path with the chat and message tokens, and the
    /// permission that the endpoint needs.
    /// </summary>
    public static TheoryData<string, string, string, string> ChatEndpoints => new()
    {
        { "Get", "GET", BasePath + "/" + ChatIdToken, ReadPermission },
        { "Messages", "GET", BasePath + "/" + ChatIdToken + "/messages?limit=10", ReadPermission },
        { "DeleteChat", "DELETE", BasePath + "/" + ChatIdToken, WritePermission },
        {
            "DeleteMessage",
            "DELETE",
            BasePath + "/" + ChatIdToken + "/messages/" + MessageIdToken,
            WritePermission
        },
    };
}
