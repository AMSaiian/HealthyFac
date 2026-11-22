namespace HealthyFac.Seeder.UnitTests.Cases.Data;

public static class CasesPartTestData
{
    public const string FileName = "cases.json";
    public const string Item = "cases.json#case";

    public static string CasesJson(string coverage) =>
        $$"""
          {
            "case": { "id": "0190f3a0-0000-7000-8000-000000000c41", "machine": "lathe", "openedAt": "h-6" },
            "signal": {
              "id": "0190f3a0-0000-7000-8000-000000000c42",
              "rule": "threshold",
              "severity": "warning",
              "state": "open",
              "coverage": "{{coverage}}"
            },
            "workOrderDraft": {
              "draftId": "0190f3a0-0000-7000-8000-000000000c21",
              "runId": "0190f3a0-0000-7000-8000-000000000c31",
              "title": "Inspect the spindle bearing",
              "priority": "high",
              "category": "inspection",
              "estimateMinutes": 60
            }
          }
          """;
}
