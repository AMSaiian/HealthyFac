using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public static class ChatTestData
{
    public const string ShortText = "Why is the spindle hot?";
    public const string Summary = "The spindle runs hot after the filter change.";
    public const int TitleLength = 80;
    public const int LongTextLength = 200;

    public static readonly Guid OwnerId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e01");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e02");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000e03");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 25, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly string EmojiAtTitleLimit = new string(c: 'a', TitleLength - 1) + "😀";

    public static readonly TimeSpan TurnLimit = TimeSpan.FromSeconds(seconds: 90);

    public static ChatMessageLink[] Links =>
        [new(ChatLinkKind.Machine, MachineId, label: "Lathe 7", position: 4)];

    public static TheoryData<string, ChatMessageState, AgentRunErrorCode?> FinalStates => new()
    {
        { "Cancelled", ChatMessageState.Cancelled, null },
        { "Failed", ChatMessageState.Failed, AgentRunErrorCode.TimeLimit },
        { "Interrupted", ChatMessageState.Interrupted, null },
    };

    public static TheoryData<string, ChatMessageState> StatesThatDoNotEndAReply => new()
    {
        { "Streaming", ChatMessageState.Streaming },
        { "Completed", ChatMessageState.Completed },
    };

    public static TheoryData<string, int, bool> DeadChecks => new()
    {
        { "AtTheLimit", 90, false },
        { "AboveTheLimit", 91, true },
    };
}
