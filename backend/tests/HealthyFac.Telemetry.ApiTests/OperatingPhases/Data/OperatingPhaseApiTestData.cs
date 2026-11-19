namespace HealthyFac.Telemetry.ApiTests.OperatingPhases.Data;

public static class OperatingPhaseApiTestData
{
    public const string MachinesPath = "v1/machines";
    public const string OperatingPhasesSegment = "operating-phases";
    public const string OrderSegment = "order";
    public const string ReadPermission = "operating-phases:read";
    public const string WritePermission = "operating-phases:write";
    public const string Code = "cutting";
    public const string StatusCode = "S002";
    public const string SomeEncryptedId = "not-checked-before-the-policy";

    /// <summary>Each route with the permission that it does not accept: method, path suffix, and the body.</summary>
    public static TheoryData<string, string, string, string, string?> RoutesWithOtherPermission => new()
    {
        { "List with the write permission", "GET", "", WritePermission, null },
        { "Create with the read permission", "POST", "", ReadPermission, "{}" },
        { "Reorder with the read permission", "PUT", "/" + OrderSegment, ReadPermission, "{}" },
        { "Get with the write permission", "GET", "/" + SomeEncryptedId, WritePermission, null },
        { "Update with the read permission", "PUT", "/" + SomeEncryptedId, ReadPermission, "{}" },
        { "Delete with the read permission", "DELETE", "/" + SomeEncryptedId, ReadPermission, null },
    };
}
