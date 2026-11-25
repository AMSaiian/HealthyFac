using HealthyFac.Insights.Application.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatSendFixtureExtensions
{
    extension(ChatTurnFixture fixture)
    {
        public async Task<ChatTurnResponse> SendAsync(
            Guid userId,
            Guid chatId,
            string text,
            IReadOnlyList<ChatLinkInput>? links = null
        )
        {
            using var scope = fixture.Inner.CreateChatUserScope(userId, ChatTestData.CompanyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SendChatMessageRequest, ChatTurnResponse>>()
                .HandleAsync(
                    new SendChatMessageRequest
                    {
                        CompanyId = ChatTestData.CompanyId,
                        ChatId = chatId,
                        Text = text,
                        Links = links ?? [],
                    });
        }

        public async Task<ChatTurnResponse> CreateAsync(
            Guid userId,
            string text,
            IReadOnlyList<ChatLinkInput>? links = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using var scope = fixture.Inner.CreateChatUserScope(userId, ChatTestData.CompanyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateChatRequest, ChatTurnResponse>>()
                .HandleAsync(
                    new CreateChatRequest { CompanyId = ChatTestData.CompanyId, Text = text, Links = links ?? [] },
                    cancellationToken);
        }

        public async Task<int> CountChatsAsync()
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return await dbContext.Chats.CountAsync();
        }

        public async Task<IReadOnlyList<(int Sequence, ChatMessageState State)>> ListMessageStatesAsync(Guid chatId)
        {
            using var scope = fixture.Inner.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            var messages = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ChatId == chatId)
                .OrderBy(message => message.Sequence)
                .ToListAsync();

            return [.. messages.Select(message => (message.Sequence, message.State))];
        }
    }
}
