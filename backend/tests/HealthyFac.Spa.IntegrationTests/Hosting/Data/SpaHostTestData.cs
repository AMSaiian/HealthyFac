namespace HealthyFac.Spa.IntegrationTests.Hosting.Data;

public static class SpaHostTestData
{
    public static TheoryData<string, string> NoCacheFileNames => new()
    {
        { "hash-like name without a dot", SpaFactory.LookalikeFileName },
        { "unhashed name", SpaFactory.UnhashedFileName },
    };
}
