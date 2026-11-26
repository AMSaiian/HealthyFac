using System.Net;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

public static class CoreWorkOrderCatalogTestData
{
    public static TheoryData<HttpStatusCode> ServerErrors =>
    [
        HttpStatusCode.InternalServerError,
        HttpStatusCode.ServiceUnavailable,
    ];

    public static TheoryData<string, Func<Exception>> NetworkFaults => new()
    {
        { "core cannot be reached", () => new HttpRequestException(message: "refused") },
        { "the call times out", () => new TaskCanceledException(message: "timeout") },
    };
}
