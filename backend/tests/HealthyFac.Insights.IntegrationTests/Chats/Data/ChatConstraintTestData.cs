using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatConstraintTestData
{
    public const string Text = "Why is the spindle hot?";

    public static readonly string EmojiAtTitleLimit = new string(c: 'a', Chat.TitleMaxLength - 1) + "😀";

    public static readonly Guid OwnerId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f01");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f02");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 25, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    public static Chat CreateChat() => new(OwnerId, CompanyId, Now);

    public static Chat CreateChatWithTurn()
    {
        var chat = CreateChat();
        chat.AddTurn(Text, [], Now);

        return chat;
    }

    extension(InsightsFixture fixture)
    {
        public async Task ResetNextSequenceAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            await dbContext.Database.ExecuteSqlAsync($"UPDATE chats SET next_sequence = 1 WHERE id = {chatId}");
        }

        public async Task SetChatStateAsync(Guid chatId, string state)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            await dbContext.Database.ExecuteSqlAsync($"UPDATE chats SET state = {state} WHERE id = {chatId}");
        }

        public async Task AddTurnAsync(Guid chatId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            var chat = await dbContext.Chats.Include(candidate => candidate.Messages)
                .SingleAsync(candidate => candidate.Id == chatId);

            chat.AddTurn(Text, [], Now);
            await dbContext.SaveChangesAsync();
        }
    }
}
