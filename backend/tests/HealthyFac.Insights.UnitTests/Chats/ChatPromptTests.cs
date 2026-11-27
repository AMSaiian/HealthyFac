using System.Text.RegularExpressions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Agents.Prompts;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatPromptTests
{
    private readonly string _text = new EmbeddedAgentPrompts().For(AgentRole.Chat).Text;

    [Theory]
    [MemberData(nameof(ChatPromptTestData.Headings), MemberType = typeof(ChatPromptTestData))]
    public void ChatPrompt_HoldsTheHeadingOfEachGroundingSection(string caseName, string heading)
    {
        _text.Split(separator: '\n').Should().Contain(heading, caseName);
    }

    [Theory]
    [MemberData(nameof(ChatPromptTestData.Rules), MemberType = typeof(ChatPromptTestData))]
    public void ChatPrompt_HoldsEachRule(string caseName, string rule)
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(rule, caseName);
    }
}
