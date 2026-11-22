namespace HealthyFac.Seeder.UnitTests.People.Data;

public static class PeoplePartTestData
{
    public const string FileName = "company.json";
    public const string Item = "company.json#anna.accepted";

    public static TheoryData<string, string, string> InvalidAcceptMoments => new()
    {
        { "an accept in the hours form", "h-3", "needs a moment with a time" },
        { "an accept 30 minutes before midnight", "d-1 23:45", "pass midnight" },
    };

    public static string CompanyJson(string acceptedAt) =>
        $$"""
          {
            "company": {
              "id": "019b4c00-0000-7000-8000-00000000c001",
              "name": "Kalyna",
              "timeZoneId": "Europe/Kyiv",
              "createdAt": "d-61 08:00"
            },
            "password": "Kalyna-Demo-2026!",
            "people": [
              {
                "key": "anna",
                "firstName": "Anna",
                "lastName": "Test",
                "email": "anna.test@kalyna-works.test",
                "role": "supervisor",
                "invitedAt": "d-62 08:05",
                "invitedBy": null,
                "acceptedAt": "{{acceptedAt}}"
              }
            ],
            "openInvites": [],
            "shiftOverrides": []
          }
          """;
}
