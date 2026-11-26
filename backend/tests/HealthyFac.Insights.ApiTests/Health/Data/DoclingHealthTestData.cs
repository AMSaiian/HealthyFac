using System.Net;

namespace HealthyFac.Insights.ApiTests.Health.Data;

public static class DoclingHealthTestData
{
    public static TheoryData<string, Func<FakeDoclingHealthHandler>, string> Answers => new()
    {
        { "answering", () => new FakeDoclingHealthHandler { Status = HttpStatusCode.OK }, "Healthy" },
        { "down", () => new FakeDoclingHealthHandler(), "Degraded" },
        {
            "answering an error",
            () => new FakeDoclingHealthHandler { Status = HttpStatusCode.InternalServerError },
            "Degraded"
        },
    };
}
