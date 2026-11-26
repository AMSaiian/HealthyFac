using System.Net;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

public static class CoreWorkOrderCatalogTestData
{
    public static TheoryData<HttpStatusCode> ServerErrors =>
    [
        HttpStatusCode.InternalServerError,
        HttpStatusCode.ServiceUnavailable,
    ];
}
