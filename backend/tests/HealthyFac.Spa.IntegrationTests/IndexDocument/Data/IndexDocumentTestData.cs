namespace HealthyFac.Spa.IntegrationTests.IndexDocument.Data;

public static class IndexDocumentTestData
{
    public static TheoryData<string, string> WebSocketOrigins =>
        new()
        {
            {
                "https://api.s3.hfverify.test:9446/",
                "connect-src 'self' https://api.s3.hfverify.test:9446 wss://api.s3.hfverify.test:9446 "
                + "https://auth.healthyfac.test;"
            },
            {
                "http://api.localhost:5000/",
                "connect-src 'self' http://api.localhost:5000 ws://api.localhost:5000 https://auth.healthyfac.test;"
            },
        };
}
