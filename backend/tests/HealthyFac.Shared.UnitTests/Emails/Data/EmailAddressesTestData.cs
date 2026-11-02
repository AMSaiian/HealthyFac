namespace HealthyFac.Shared.UnitTests.Emails.Data;

public static class EmailAddressesTestData
{
    public static TheoryData<string?, string?> Emails =>
        new()
        {
            { "User@Example.COM", "user@example.com" },
            { "  user@example.com  ", "user@example.com" },
            { " Mixed.Case@HealthyFac.Test\t", "mixed.case@healthyfac.test" },
            { "user@example.com", "user@example.com" },
            { null, null },
        };
}
