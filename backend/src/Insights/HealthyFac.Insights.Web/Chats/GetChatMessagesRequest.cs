using HealthyFac.Shared.Application.IdEncryption;
using Microsoft.AspNetCore.Mvc;
using ApplicationGetChatMessagesRequest = HealthyFac.Insights.Application.Chats.GetChatMessagesRequest;

namespace HealthyFac.Insights.Web.Chats;

public record GetChatMessagesRequest
{
    [FromRoute(Name = ChatRoutes.ChatIdParameter)]
    public required string ChatId { get; init; }

    [FromQuery(Name = ChatRoutes.LimitParameter)]
    public required int Limit { get; init; }

    [FromQuery(Name = ChatRoutes.BeforeParameter)]
    public int? Before { get; init; }
}

public static class GetChatMessagesRequestExtensions
{
    extension(GetChatMessagesRequest request)
    {
        public ApplicationGetChatMessagesRequest ToApplicationRequest(Guid companyId, IIdEncryptor idEncryptor) =>
            new()
            {
                CompanyId = companyId,
                ChatId = ChatRouteIds.DecryptChat(request.ChatId, idEncryptor),
                Limit = request.Limit,
                Before = request.Before,
            };
    }
}
