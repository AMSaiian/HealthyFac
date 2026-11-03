namespace HealthyFac.Spa.IntegrationTests.Hosting.Data;

public static class SpaOptionsTestData
{
    public static TheoryData<string> InvalidApiBaseUrls => new()
    {
        "http://api.healthyfac.test",
        "https://localhost",
        "https://api.healthyfac.test/",
    };

    public static TheoryData<string> InvalidAuthUrls => new()
    {
        "http://auth.healthyfac.test",
        "https://localhost",
        "https://auth.healthyfac.test/",
        "",
        " ",
    };

    public static TheoryData<string> BlankValues => new()
    {
        "",
        " ",
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

    public static TheoryData<int> RuntimeConfigCacheSecondsOutsideRange => new()
    {
        0,
        3601,
    };

    public static TheoryData<int> RuntimeConfigCacheSecondsAtEdges => new()
    {
        1,
        3600,
    };

    public static TheoryData<int> CacheSecondsAtEdges => new()
    {
        1,
        31536000,
    };
}
