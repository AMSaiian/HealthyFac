using System.Text.Json.Nodes;

namespace HealthyFac.Core.ApiTests.Drafts.Data;

public static class DraftApiTestData
{
    public const string MachineDraftsPath = "v1/machine-drafts";
    public const string DraftsPath = "v1/drafts";
    public const string MachineDraftNotAppliedKey = "machineDraftNotApplied";
    public const string DraftWritePermission = "drafts:write";
    public const string DraftReadPermission = "drafts:read";
    public const string MachineWritePermission = "machines:write";
    public const string MachineReadPermission = "machines:read";
    public const string ProposedName = "AX-200 Rotary Pump";
    public const string ProposedSerialNumber = "AX-200-001";
    public const string MetricKey = "motor.temperature";
    public const string StatusCode = "E104";
    public const string DraftNotPendingKey = "draftNotPending";

    public static readonly Guid AttemptId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");
    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a02");

    public static readonly DateTime QueuedAt =
        new(year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, DateTimeKind.Utc);

    public static string ApplyPath(string encryptedDraftId) => $"{MachineDraftsPath}/{encryptedDraftId}/apply";

    public static string DismissPath(string encryptedDraftId) => $"{DraftsPath}/{encryptedDraftId}/dismiss";

    public static string MarkAppliedPath => $"{DraftsPath}/mark-applied";

    public static string DismissMachineDraftsPath(string encryptedMachineId) =>
        $"v1/machines/{encryptedMachineId}/drafts/dismiss";

    public static TheoryData<string, string> DismissRoutesWithoutWrite => new()
    {
        { "DismissDraft", DismissPath("{draftId}") },
        { "DismissMachineDrafts", DismissMachineDraftsPath("{machineId}") },
    };

    public const string MarkBodyWithNullIds = "{\"ids\": null}";

    public static string MarkBody(params string[] encryptedIds) =>
        new JsonObject { [propertyName: "ids"] = new JsonArray([.. encryptedIds.Select(id => JsonValue.Create(id))]) }
            .ToJsonString();

    public static string ApplyBody(string name, string serialNumber, uint? rowVersion = null)
    {
        var body = new JsonObject
        {
            [propertyName: "name"] = name,
            [propertyName: "serialNumber"] = serialNumber,
        };

        if (rowVersion is { } version)
        {
            body[propertyName: "rowVersion"] = version;
        }

        return body.ToJsonString();
    }
}
