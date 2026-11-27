using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatTestData
{
    public const string Question = "Why is the spindle hot?";
    public const string Answer = "Check the filter.";
    public const int PageLimit = 4;

    public const string OwnerName = "Ann Owner";

    public static readonly Guid OwnerId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f11");
    public static readonly Guid OtherUserId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f12");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f13");
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f14");
    public const string MachineName = "Lathe 7";

    public static GetMachineResponse MachineAnswer() =>
        new()
        {
            Machine = new ChatMachine
            {
                Id = MachineId,
                Name = MachineName,
                SerialNumber = "SN-7",
                State = "operational",
                ExpectedGoLiveOn = "2026-01-01",
            },
            Metrics = [],
            Statuses = [],
        };

    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f15");
    public static readonly Guid DraftId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000f16");

    public static readonly DateTime Now = new(
        year: 2026, month: 11, day: 25, hour: 10, minute: 0, second: 0, DateTimeKind.Utc);

    public static ChatMessageLink[] Links =>
        [new(ChatLinkKind.Machine, MachineId, label: "Lathe 7", position: 4)];
}
