using HealthyFac.Insights.Application.Chats;
using ApplicationCreateChatRequest = HealthyFac.Insights.Application.Chats.CreateChatRequest;

namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// The body of the creation of a chat: the text and the links of the first message. The company comes from the token.
/// </summary>
public record CreateChatRequest
{
    public required string Text { get; init; }

    public IReadOnlyList<ChatLinkInput> Links
    {
        get;
        init => field = value ?? [];
    } = [];
}

public static class CreateChatRequestExtensions
{
    extension(CreateChatRequest request)
    {
        public ApplicationCreateChatRequest ToApplicationRequest(Guid companyId) =>
            new()
            {
                CompanyId = companyId,
                Text = request.Text,
                Links = request.Links,
            };
    }
}
