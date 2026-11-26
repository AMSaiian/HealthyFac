namespace HealthyFac.Shared.UnitTests.CrossOrigin.Data;

public static class CorsTestData
{
    public static TheoryData<string, string, string> RejectedPreflights => new()
    {
        { "other origin", CorsHostFactory.EndpointWithPolicy, CorsHostFactory.OtherOrigin },
        { "endpoint without a policy", CorsHostFactory.EndpointWithoutPolicy, CorsHostFactory.AllowedOrigin },
    };
}
