namespace HealthyFac.Spa.IntegrationTests.Hosting.Data;

public static class SpaOptionsTestData
{
    public static TheoryData<string> InvalidApiBaseUrls =>
    [
        "http://api.healthyfac.test",
        "https://localhost",
        "https://api.healthyfac.test/",
    ];

    public static TheoryData<string> InvalidAuthUrls =>
    [
        "http://auth.healthyfac.test",
        "https://localhost",
        "https://auth.healthyfac.test/",
        "",
        " ",
    ];

    public static TheoryData<string> InvalidHashPatterns =>
    [
        "[",
        "",
        " ",
    ];

    public static TheoryData<int> CacheSecondsOutsideRange =>
    [
        0,
        31536001,
    ];

    public static TheoryData<int> RuntimeConfigCacheSecondsOutsideRange =>
    [
        0,
        3601,
    ];

    public static TheoryData<int> RuntimeConfigCacheSecondsAtEdges =>
    [
        1,
        3600,
    ];

    public static TheoryData<int> CacheSecondsAtEdges =>
    [
        1,
        31536000,
    ];
}
