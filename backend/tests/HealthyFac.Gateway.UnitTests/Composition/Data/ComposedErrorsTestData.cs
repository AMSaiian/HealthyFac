using System.Text.Json.Nodes;
using HealthyFac.Gateway.Web.Composition;

namespace HealthyFac.Gateway.UnitTests.Composition.Data;

public static class ComposedErrorsTestData
{
    public const string CoreTitle = "core";
    public const string TelemetryTitle = "telemetry";

    /// <summary>
    /// The case name, the results of Core and Telemetry, the expected status, and the title of the body that the
    /// answer carries. The title names the source that supplied the body.
    /// </summary>
    public static TheoryData<string, DownstreamResult[], int, string> Failures => new()
    {
        {
            "unavailable and success",
            [new DownstreamResult.Success { Body = new JsonObject() }, new DownstreamResult.Unavailable()],
            503, "Service Unavailable"
        },
        {
            "unavailable and a client error",
            [Error(status: 403, CoreTitle), new DownstreamResult.Unavailable()],
            503, "Service Unavailable"
        },
        { "403 and 404", [Error(status: 404, CoreTitle), Error(status: 403, TelemetryTitle)], 403, TelemetryTitle },
        { "401 and 403", [Error(status: 403, CoreTitle), Error(status: 401, TelemetryTitle)], 401, TelemetryTitle },
        { "404 and 409", [Error(status: 409, CoreTitle), Error(status: 404, TelemetryTitle)], 404, TelemetryTitle },
        { "409 and 400", [Error(status: 400, CoreTitle), Error(status: 409, TelemetryTitle)], 409, TelemetryTitle },
        { "400 and 422", [Error(status: 422, CoreTitle), Error(status: 400, TelemetryTitle)], 400, TelemetryTitle },
        { "422 and 429", [Error(status: 422, CoreTitle), Error(status: 429, TelemetryTitle)], 422, CoreTitle },
        { "404 and 404", [Error(status: 404, CoreTitle), Error(status: 404, TelemetryTitle)], 404, CoreTitle },
        {
            "a client error and a success",
            [new DownstreamResult.Success { Body = new JsonObject() }, Error(status: 404, TelemetryTitle)],
            404, TelemetryTitle
        },
    };

    public static DownstreamResult.ClientError Error(int status, string title, string? errorsJson = null)
    {
        var problem = new JsonObject { [propertyName: "title"] = title };

        if (errorsJson is not null)
        {
            problem[propertyName: "errors"] = JsonNode.Parse(errorsJson);
        }

        return new DownstreamResult.ClientError { Status = status, Problem = problem };
    }
}
