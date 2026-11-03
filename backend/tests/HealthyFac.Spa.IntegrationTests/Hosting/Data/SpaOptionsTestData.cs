namespace HealthyFac.Spa.IntegrationTests.Hosting.Data;

public static class SpaOptionsTestData
{
    public static TheoryData<string> InvalidApiBaseUrls => new()
    {
        "http://api.healthyfac.test",
        "https://localhost",
        "https://api.healthyfac.test/",
    };

    public static TheoryData<string> InvalidHashPatterns => new()
    {
        "[",
        "",
        " ",
    };

    public static TheoryData<int> CacheSecondsOutsideRange => new()
    {
        0,
        31536001,
    };

    public static TheoryData<int> CacheSecondsAtEdges => new()
    {
        1,
        31536000,
    };
}
