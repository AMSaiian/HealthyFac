using HealthyFac.Insights.IntegrationTests.Chats.Data;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[CollectionDefinition(Name)]
public sealed class ChatTurnCollection : ICollectionFixture<ChatTurnFixture>
{
    public const string Name = "InsightsChatTurns";
}
