namespace HealthyFac.Shared.UnitTests.Emails.Data;

public static class EmailAddressesTestData
{
    public static TheoryData<string?, string?> Emails =>
        new()
        {
            { " Mixed.Case@HealthyFac.Test\t", "mixed.case@healthyfac.test" },
            { null, null },
        };
}
