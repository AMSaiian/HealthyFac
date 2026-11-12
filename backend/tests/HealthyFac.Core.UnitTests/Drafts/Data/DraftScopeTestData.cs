namespace HealthyFac.Core.UnitTests.Drafts.Data;

public static class DraftScopeTestData
{
    private static readonly Guid Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01");

    /// <summary>The case name, the machine ID, the attempt ID, and whether the request is valid.</summary>
    public static TheoryData<string, Guid?, Guid?, bool> Scopes => new()
    {
        { "MachineOnly", Id, null, true },
        { "AttemptOnly", null, Id, true },
        { "Both", Id, Id, false },
        { "Neither", null, null, false },
    };
}
