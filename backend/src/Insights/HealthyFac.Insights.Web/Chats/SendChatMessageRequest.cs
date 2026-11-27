using HealthyFac.Insights.Application.Chats;
using ApplicationSendChatMessageRequest = HealthyFac.Insights.Application.Chats.SendChatMessageRequest;

namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// The body of a send: the text and the links of the message. The chat and the company come from the route and the
/// token.
/// </summary>
public record SendChatMessageRequest
{
    public required string Text { get; init; }

    public IReadOnlyList<ChatLinkInput> Links
    {
        get;
        init => field = value ?? [];
    } = [];
}

public static class SendChatMessageRequestExtensions
{
    extension(SendChatMessageRequest request)
    {
        public ApplicationSendChatMessageRequest ToApplicationRequest(Guid companyId, Guid chatId) =>
            new()
            {
                CompanyId = companyId,
                ChatId = chatId,
                Text = request.Text,
                Links = request.Links,
            };
    }
}
