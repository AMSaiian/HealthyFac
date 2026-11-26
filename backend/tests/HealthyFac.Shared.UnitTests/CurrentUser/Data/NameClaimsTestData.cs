namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class NameClaimsTestData
{
    /// <summary>
    /// The case name, the claim that the token holds first, the other claim, and the expected user name.
    /// </summary>
    public static TheoryData<string, string, string, string, string, string> NameClaims => new()
    {
        { "NameWins", "name", "Ann Owner", "preferred_username", "ann", "Ann Owner" },
        { "PreferredUsernameAfterName", "preferred_username", "ann", "email", "ann@x.test", "ann" },
        { "EmailLast", "email", "ann@x.test", "other", "none", "ann@x.test" },
        { "BlankNameSkipped", "name", "  ", "preferred_username", "ann", "ann" },
    };
}
