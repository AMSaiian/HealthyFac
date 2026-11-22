namespace HealthyFac.Core.UnitTests.Shifts.Data;

public static class TodayOverrideMergeTestData
{
    private const string Template = "08:00-12:00,13:00-17:00";
    private const string TouchingTemplate = "08:00-12:00,12:00-17:00";

    public static TheoryData<string, string, string, string, string, string> Cases => new()
    {
        { "DayOffCancelled", "", Template, "14:02", "14:05", "14:05-17:00" },
        { "FutureOverride", "10:00-14:00", Template, "06:00", "06:00", Template },
        { "RunningBlockJoins", "09:00-11:00", Template, "09:32", "09:35", "09:00-12:00,13:00-17:00" },
        { "EndedOverrideEqualsTemplate", "08:00-12:00", Template, "12:30", "12:30", Template },
        { "NowAfterLastBlock", "09:00-17:00", Template, "18:00", "18:00", "09:00-17:00" },
        {
            "TouchingOverrideBlocksStaySeparate", "08:00-10:00,10:00-12:00", Template, "13:02", "13:05",
            "08:00-10:00,10:00-12:00,13:05-17:00"
        },
        {
            "TouchingTemplateBlocksStaySeparate", TouchingTemplate, TouchingTemplate, "06:00", "06:00", TouchingTemplate
        },
        { "EndedExactlyNowStaysSeparate", "08:00-12:00", "08:00-17:00", "12:00", "12:00", "08:00-12:00,12:00-17:00" },
    };
}
