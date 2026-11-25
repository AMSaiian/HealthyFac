using System.Text.Json;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatDraftKindFileTests
{
    [Fact]
    public void KindFile_AgainstTheDomainEnumeration_HoldsTheSameValues()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, path2: "Chats", path3: "Data", path4: "chat-draft-kinds.json");

        var fileValues = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path))!;

        fileValues.Should().BeEquivalentTo(ChatDraftKind.All.Select(kind => kind.Value));
    }
}
