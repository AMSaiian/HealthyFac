using HealthyFac.Insights.Application.Chats.Turns;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public static class ChatTriageModelTestData
{
    public const string Text = "Why is the spindle of Lathe 7 hot?";

    public static readonly IReadOnlyList<string> Labels = ["Lathe 7", "WO-12"];

    public static TheoryData<string, string, ChatTriageChoice> Verdicts => new()
    {
        { "InScope", """{"verdict":"inScope"}""", ChatTriageChoice.InScope },
        { "OffTopic", """{"verdict":"offTopic"}""", ChatTriageChoice.OffTopic },
        { "Malicious", """{"verdict":"malicious"}""", ChatTriageChoice.Malicious },
    };

    /// <summary>The case name and an answer that the schema of the verdict does not allow.</summary>
    public static TheoryData<string, string> BrokenAnswers => new()
    {
        { "NoJson", "not json" },
        { "NumberAsTheVerdict", """{"verdict":1}""" },
    };
}
