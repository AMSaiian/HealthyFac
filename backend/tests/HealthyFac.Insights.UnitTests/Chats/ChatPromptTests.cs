using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Agents.Prompts;

namespace HealthyFac.Insights.UnitTests.Chats;

public sealed class ChatPromptTests
{
    private readonly string _text = new EmbeddedAgentPrompts().For(AgentRole.Chat).Text;

    [Theory]
    [InlineData("COMPANY FACTS")]
    [InlineData("GENERAL KNOWLEDGE")]
    [InlineData("NO ANSWER")]
    public void ChatPrompt_HoldsTheHeadingOfEachGroundingSection(string heading)
    {
        _text.Split(separator: '\n').Should().Contain(heading);
    }

    [Fact]
    public void ChatPrompt_NamesTheNoAnswerToolAndTheToolResultBlock()
    {
        _text.Should().Contain(expected: "report_no_answer").And.Contain(expected: "<tool_result>");
    }
}
