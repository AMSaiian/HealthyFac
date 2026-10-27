namespace HealthyFac.Core.UnitTests.Employees.InviteLinks.Data;

public static class InviteLinkOptionsValidatorTestData
{
    public static TheoryData<string> ValidBaseUrls => new()
    {
        "http://localhost:4200",
        "https://app.example",
        "https://app.example/",
        "https://app.example/portal",
    };

    public static TheoryData<string> InvalidBaseUrls => new()
    {
        string.Empty,
        "   ",
        "app.example",
        "/invite",
        "ftp://app.example",
        "mailto:admin@app.example",
    };
}
