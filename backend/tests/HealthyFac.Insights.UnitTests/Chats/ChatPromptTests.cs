using System.Text.RegularExpressions;
using HealthyFac.Insights.Application.Chats.Tools;
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
    public void ChatPrompt_NamesTheFixedTextOfAnUnavailableService()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(ChatToolErrors.ServiceUnavailable);
    }

    [Fact]
    public void ChatPrompt_NamesTheNoAnswerToolAndTheToolResultBlock()
    {
        _text.Should().Contain(expected: "report_no_answer").And.Contain(expected: "<tool_result>");
    }

    [Fact]
    public void ChatPrompt_TellsTheModelToAskAFollowUpQuestionInsteadOfReportingNoAnswer()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(expected: "ask one short follow-up question as your answer");
        oneLine.Should().Contain(expected: "Do not call the tool report_no_answer for it");
    }

    [Fact]
    public void ChatPrompt_LimitsADraftToValuesFromTheDataOrTheUser()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should()
            .Contain(expected: "Propose a draft only with values from the data of the company or from the user");
        oneLine.Should().Contain(expected: "Leave a field empty when you do not know its value");
    }

    [Fact]
    public void ChatPrompt_ForbidsNoAnswerInATurnThatProposedADraft()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should()
            .Contain(expected: "Never call the tool report_no_answer in a turn in which you proposed a draft");
    }

    [Theory]
    [InlineData("Never show an ID. Name a machine, a work order, a case, a person, or a file by its name or title.")]
    [InlineData("Write plain text. Do not use Markdown.")]
    public void ChatPrompt_HoldsTheRuleOfTheNamesAndThePlainText(string rule)
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(rule);
    }

    [Fact]
    public void ChatPrompt_TellsTheModelToAnswerAfterAProposal()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(expected: "After you propose drafts, always answer in 1 to 3 short sentences");
    }

    [Fact]
    public void ChatPrompt_TellsTheModelToSendNoUnitPairThatItGuesses()
    {
        var oneLine = Regex.Replace(_text, pattern: @"\s+", replacement: " ");

        oneLine.Should().Contain(expected: "Never retry a rejected metric draft with another guess");
    }
}
