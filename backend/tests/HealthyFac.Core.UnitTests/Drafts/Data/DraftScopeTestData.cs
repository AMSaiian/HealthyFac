namespace HealthyFac.Core.UnitTests.Drafts.Data;

public static class DraftScopeTestData
{
    private static readonly Guid Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000b01");

    /// <summary>The case name, the machine ID, the attempt ID, the chat ID, and whether the request is valid.</summary>
    public static TheoryData<string, Guid?, Guid?, Guid?, bool> Scopes => new()
    {
        { "MachineOnly", Id, null, null, true },
        { "AttemptOnly", null, Id, null, true },
        { "ChatOnly", null, null, Id, true },
        { "MachineAndAttempt", Id, Id, null, false },
        { "MachineAndChat", Id, null, Id, false },
        { "AttemptAndChat", null, Id, Id, false },
        { "AllThree", Id, Id, Id, false },
        { "Neither", null, null, null, false },
        { "EmptyChat", null, null, Guid.Empty, false },
        { "EmptyMachine", Guid.Empty, null, null, false },
        { "EmptyAttempt", null, Guid.Empty, null, false },
    };

    /// <summary>The case name, the attempt ID, the chat ID, and whether the grouped read is valid.</summary>
    public static TheoryData<string, Guid?, Guid?, bool> GetScopes => new()
    {
        { "AttemptOnly", Id, null, true },
        { "ChatOnly", null, Id, true },
        { "Both", Id, Id, false },
        { "Neither", null, null, false },
        { "EmptyChat", null, Guid.Empty, false },
        { "EmptyAttempt", Guid.Empty, null, false },
    };
}
