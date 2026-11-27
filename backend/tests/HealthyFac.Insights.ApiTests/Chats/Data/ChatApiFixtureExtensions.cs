using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Testing.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Chats.Data;

public static class ChatApiFixtureExtensions
{
    extension(InsightsApiFixture fixture)
    {
        public TestTokenClaims CreateChatClaims(Guid userId, params string[] permissions) =>
            new()
            {
                Subject = userId,
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        /// <summary>
        /// Saves a chat with one completed turn for the owner.
        /// </summary>
        /// <param name="ownerId">The user who owns the chat.</param>
        /// <returns>The chat with its messages.</returns>
        public async Task<Chat> ArrangeChatAsync(Guid ownerId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var chat = new Chat(ownerId, fixture.CompanyId, ChatApiTestData.Now);
            var (_, reply) = chat.AddTurn(ChatApiTestData.Question, [], ChatApiTestData.Now);
            reply.Complete(ChatMessageKind.Answer, ChatApiTestData.Answer);

            dbContext.Chats.Add(chat);
            await dbContext.SaveChangesAsync();

            return chat;
        }

        public async Task SoftDeleteChatAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
            var chat = await dbContext.Chats.SingleAsync(candidate => candidate.Id == chatId);

            dbContext.Chats.Remove(chat);
            await dbContext.SaveChangesAsync();
        }

        public string Encrypt(Guid id) => fixture.IdEncryptor.Encrypt(id);
    }
}
