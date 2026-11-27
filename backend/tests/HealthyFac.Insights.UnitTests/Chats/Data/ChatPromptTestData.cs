using HealthyFac.Insights.Application.Chats.Tools;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public static class ChatPromptTestData
{
    /// <summary>The case name and the heading of one grounding section.</summary>
    public static TheoryData<string, string> Headings => new()
    {
        { "CompanyFacts", "COMPANY FACTS" },
        { "GeneralKnowledge", "GENERAL KNOWLEDGE" },
        { "NoAnswer", "NO ANSWER" },
    };

    /// <summary>The case name and a rule of the prompt, with the line breaks of the prompt read as spaces.</summary>
    public static TheoryData<string, string> Rules => new()
    {
        {
            "NamesNotIds",
            "Never show an ID. Name a machine, a work order, a case, a person, or a file by its name or title."
        },
        { "PlainText", "Write plain text. Do not use Markdown." },
        { "UnavailableService", ChatToolErrors.ServiceUnavailable },
        { "NoAnswerTool", "report_no_answer" },
        { "ToolResultBlock", "<tool_result>" },
        { "FollowUpQuestion", "ask one short follow-up question as your answer" },
        { "NoAnswerToolForFollowUp", "Do not call the tool report_no_answer for it" },
        { "DraftValuesFromData", "Propose a draft only with values from the data of the company or from the user" },
        { "EmptyUnknownField", "Leave a field empty when you do not know its value" },
        { "NoAnswerAfterDraft", "Never call the tool report_no_answer in a turn in which you proposed a draft" },
        { "AnswerAfterProposal", "After you propose drafts, always answer in 1 to 3 short sentences" },
        { "NoGuessedRetry", "Never retry a rejected metric draft with another guess" },
    };
}
