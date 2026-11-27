using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ApplicationCreateChatRequest = HealthyFac.Insights.Application.Chats.CreateChatRequest;
using ApplicationGetChatMessagesRequest = HealthyFac.Insights.Application.Chats.GetChatMessagesRequest;
using ApplicationSearchChatsRequest = HealthyFac.Insights.Application.Chats.SearchChatsRequest;
using ApplicationSendChatMessageRequest = HealthyFac.Insights.Application.Chats.SendChatMessageRequest;

namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// Configures endpoint routing for the chats of the user. Reading needs <c>chats:read</c>. Sending, creating, and
/// deleting need <c>chats:write</c>. Every endpoint reaches only the chats of the user in the token.
/// </summary>
public static class ChatEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps the chat endpoints to the application route builder.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapChatEndpoints()
        {
            var group = app.MapGroup(ChatRoutes.BaseRoute);
            group.MapPost(ChatRoutes.Search, SearchAsync).RequireAuthorization(InsightsPermissions.Chats.Read);
            group.MapGet(ChatRoutes.ById, GetAsync).RequireAuthorization(InsightsPermissions.Chats.Read);
            group.MapGet(ChatRoutes.Messages, GetMessagesAsync).RequireAuthorization(InsightsPermissions.Chats.Read);
            group.MapPost(ChatRoutes.Root, CreateAsync).RequireAuthorization(InsightsPermissions.Chats.Write);
            group.MapPost(ChatRoutes.Messages, SendAsync).RequireAuthorization(InsightsPermissions.Chats.Write);
            group.MapDelete(ChatRoutes.ById, DeleteAsync).RequireAuthorization(InsightsPermissions.Chats.Write);

            group.MapDelete(ChatRoutes.MessageById, DeleteMessageAsync)
                .RequireAuthorization(InsightsPermissions.Chats.Write);

            return app;
        }
    }

    private static async Task<Ok<KeysetGridResponse<ChatListItem>>> SearchAsync(
        SearchChatsRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSearchChatsRequest, KeysetGridResponse<ChatListItem>> handler,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<ChatResponse>> GetAsync(
        [FromRoute(Name = ChatRoutes.ChatIdParameter)] string chatId,
        ICurrentUserProvider currentUser,
        IRequestHandler<GetChatRequest, ChatResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            new GetChatRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                ChatId = ChatRouteIds.DecryptChat(chatId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<ChatMessagesResponse>> GetMessagesAsync(
        [AsParameters] GetChatMessagesRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationGetChatMessagesRequest, ChatMessagesResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), idEncryptor),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<Created<ChatTurnResponse>> CreateAsync(
        CreateChatRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationCreateChatRequest, ChatTurnResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId()),
            cancellationToken);

        return TypedResults.Created(
            $"{ChatRoutes.BaseRoute}/{idEncryptor.Encrypt(response.Chat.Id)}",
            response);
    }

    private static async Task<Ok<ChatTurnResponse>> SendAsync(
        [FromRoute(Name = ChatRoutes.ChatIdParameter)] string chatId,
        SendChatMessageRequest request,
        ICurrentUserProvider currentUser,
        IRequestHandler<ApplicationSendChatMessageRequest, ChatTurnResponse> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        var response = await handler.HandleAsync(
            request.ToApplicationRequest(currentUser.GetCompanyId(), ChatRouteIds.DecryptChat(chatId, idEncryptor)),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<NoContent> DeleteAsync(
        [FromRoute(Name = ChatRoutes.ChatIdParameter)] string chatId,
        ICurrentUserProvider currentUser,
        IRequestHandler<DeleteChatRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            new DeleteChatRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                ChatId = ChatRouteIds.DecryptChat(chatId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeleteMessageAsync(
        [FromRoute(Name = ChatRoutes.ChatIdParameter)] string chatId,
        [FromRoute(Name = ChatRoutes.MessageIdParameter)]
        string messageId,
        ICurrentUserProvider currentUser,
        IRequestHandler<DeleteChatMessageRequest> handler,
        IIdEncryptor idEncryptor,
        CancellationToken cancellationToken
    )
    {
        await handler.HandleAsync(
            new DeleteChatMessageRequest
            {
                CompanyId = currentUser.GetCompanyId(),
                ChatId = ChatRouteIds.DecryptChat(chatId, idEncryptor),
                MessageId = ChatRouteIds.DecryptMessage(messageId, idEncryptor),
            },
            cancellationToken);

        return TypedResults.NoContent();
    }
}
