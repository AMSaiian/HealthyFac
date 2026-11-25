using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// Arranges chats and reads them through the use cases as a user of one company.
/// </summary>
public static class ChatFixtureExtensions
{
    extension(InsightsFixture fixture)
    {
        /// <summary>
        /// Saves a chat with the given number of completed turns. Each user message has a text and the first one a
        /// link, and each reply is completed.
        /// </summary>
        public async Task<Chat> SaveChatAsync(
            Guid ownerId,
            Guid companyId,
            int turnCount,
            string? title = null,
            Action<Chat>? arrange = null
        )
        {
            var chat = new Chat(ownerId, companyId, ChatTestData.Now);
            for (var turn = 0; turn < turnCount; turn++)
            {
                var (_, reply) = chat.AddTurn(
                    title ?? $"{ChatTestData.Question} {turn}",
                    turn == 0 ? ChatTestData.Links : [],
                    ChatTestData.Now);
                if (turn == 0)
                {
                    reply.AddDraft(
                        new ChatMessageDraft(
                            ChatDraftKind.Metric, ChatTestData.DraftId, title: "Spindle temperature",
                            targetMachineId: null));
                }

                reply.Complete(ChatMessageKind.Answer, $"{ChatTestData.Answer} {turn}");
            }

            arrange?.Invoke(chat);
            await fixture.SaveAsync(chat);

            return chat;
        }

        public async Task<Chat> LoadChatAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.Chats
                .AsNoTracking()
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Include(chat => chat.Messages)
                .SingleAsync(chat => chat.Id == chatId);
        }

        public async Task<IReadOnlyList<int>> ListLiveMessageSequencesAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ChatId == chatId)
                .OrderBy(message => message.Sequence)
                .Select(message => message.Sequence)
                .ToListAsync();
        }

        public async Task<int> CountDeletedLinksAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.Database
                .SqlQuery<int>(
                    $"SELECT count(*)::int AS \"Value\" FROM chat_message_links WHERE deleted_at IS NOT NULL")
                .SingleAsync();
        }

        public async Task<int> CountDeletedDraftsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.Database
                .SqlQuery<int>(
                    $"SELECT count(*)::int AS \"Value\" FROM chat_message_drafts WHERE deleted_at IS NOT NULL")
                .SingleAsync();
        }

        public async Task DeleteChatUseCaseAsync(Guid userId, Guid companyId, Guid chatId)
        {
            using var scope = fixture.CreateChatUserScope(userId, companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteChatRequest>>()
                .HandleAsync(new DeleteChatRequest { CompanyId = companyId, ChatId = chatId });
        }

        public async Task DeleteChatMessageUseCaseAsync(Guid userId, Guid companyId, Guid chatId, Guid messageId)
        {
            using var scope = fixture.CreateChatUserScope(userId, companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteChatMessageRequest>>()
                .HandleAsync(
                    new DeleteChatMessageRequest { CompanyId = companyId, ChatId = chatId, MessageId = messageId });
        }

        public async Task SoftDeleteChatAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            await dbContext.Database.ExecuteSqlAsync(
                $"UPDATE chats SET deleted_at = now() WHERE id = {chatId}");
        }

        public async Task SoftDeleteMessageAsync(Guid messageId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            await dbContext.Database.ExecuteSqlAsync(
                $"UPDATE chat_messages SET deleted_at = now() WHERE id = {messageId}");
        }

        public async Task<KeysetGridResponse<ChatListItem>> SearchChatsUseCaseAsync(
            Guid userId,
            Guid companyId,
            int limit,
            KeysetPosition? after = null
        )
        {
            using var scope = fixture.CreateChatUserScope(userId, companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchChatsRequest, KeysetGridResponse<ChatListItem>>>()
                .HandleAsync(new SearchChatsRequest { CompanyId = companyId, Limit = limit, After = after });
        }

        public async Task<ChatResponse> GetChatUseCaseAsync(Guid userId, Guid companyId, Guid chatId)
        {
            using var scope = fixture.CreateChatUserScope(userId, companyId);

            return await scope.ServiceProvider.GetRequiredService<IRequestHandler<GetChatRequest, ChatResponse>>()
                .HandleAsync(new GetChatRequest { CompanyId = companyId, ChatId = chatId });
        }

        public async Task<ChatMessagesResponse> GetChatMessagesUseCaseAsync(
            Guid userId,
            Guid companyId,
            Guid chatId,
            int limit,
            int? before = null
        )
        {
            using var scope = fixture.CreateChatUserScope(userId, companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetChatMessagesRequest, ChatMessagesResponse>>()
                .HandleAsync(
                    new GetChatMessagesRequest
                    {
                        CompanyId = companyId,
                        ChatId = chatId,
                        Limit = limit,
                        Before = before,
                    });
        }

        public IServiceScope CreateChatUserScope(Guid userId, Guid companyId) =>
            fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = userId, CompanyId = companyId });
    }
}
