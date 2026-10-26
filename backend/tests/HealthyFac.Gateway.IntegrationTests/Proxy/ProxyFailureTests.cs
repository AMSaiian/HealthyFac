using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using HealthyFac.Gateway.Web.Proxy;
using HealthyFac.Testing.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

public sealed class ProxyFailureTests(GatewayEnvironment environment) : IClassFixture<GatewayEnvironment>
{
    [Fact]
    public async Task CoreRoute_WhenDestinationIsDown_Returns502ProblemDetails()
    {
        await using var gateway = environment.CreateGateway(ProxyFailureTestData.ClosedPortAddress);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(ProxyFailureTestData.MachinesRoute);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ProxyFailureTestData.ProblemJsonMediaType);
        problem!.Title.Should().Be(GatewayProblemDetailsConstants.BadGateway.Title);
    }

    [Fact]
    public async Task CoreRoute_WhenDestinationTimesOut_Returns504ProblemDetails()
    {
        await using var core = new EchoServer(responseDelay: ProxyFailureTestData.SlowResponseDelay);
        await core.StartAsync();
        await using var gateway = environment.CreateGateway(
            core,
            settings: ProxyFailureTestData.OneSecondActivityTimeout);
        using var client = gateway.CreateClient();
        var startedAt = Stopwatch.GetTimestamp();

        using var response = await client.GetAsync(ProxyFailureTestData.MachinesRoute);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        problem!.Title.Should().Be(GatewayProblemDetailsConstants.GatewayTimeout.Title);
        elapsed.Should().BeLessThan(ProxyFailureTestData.TimeoutResponseLimit);
    }

    [Fact]
    public async Task CoreRoute_WhenDestinationIsUnhealthy_Returns503ProblemDetails()
    {
        await using var core = new EchoServer();
        core.IsReady = false;
        await core.StartAsync();
        await using var gateway = environment.CreateGateway(
            core,
            settings: ProxyFailureTestData.OneSecondHealthCheckInterval);
        using var client = gateway.CreateClient();
        await client.WaitForStatusAsync(
            ProxyFailureTestData.MachinesRoute,
            HttpStatusCode.ServiceUnavailable,
            ProxyFailureTestData.UnhealthyWaitLimit);
        var requestCountBeforeAct = core.RequestCount;

        using var response = await client.GetAsync(ProxyFailureTestData.MachinesRoute);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        problem!.Title.Should().Be(GatewayProblemDetailsConstants.ServiceUnavailable.Title);
        core.RequestCount.Should().Be(requestCountBeforeAct);
    }

    [Fact]
    public async Task CoreRoute_WhenDestinationFails_LogsWarningWithoutAuthorizationHeader()
    {
        var logs = new CapturingLoggerProvider();
        await using var gateway = environment.CreateGateway(
            ProxyFailureTestData.ClosedPortAddress,
            configureServices: services => services.AddSingleton<ILoggerFactory>(new LoggerFactory([logs])));
        using var client = gateway.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProxyFailureTestData.MachinesRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ProxyFailureTestData.SecretToken);

        using var response = await client.SendAsync(request);

        var warnings = logs.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
        var leakedEntries = logs.Entries
            .Where(entry => entry.Message.Contains(ProxyFailureTestData.SecretToken)
                || entry.Properties.Values.Any(value =>
                    value?.ToString()?.Contains(ProxyFailureTestData.SecretToken) == true)
                || entry.Exception?.ToString().Contains(ProxyFailureTestData.SecretToken) == true)
            .ToList();

        warnings.Should()
            .Contain(entry =>
                entry.Properties.ContainsKey(ProxyFailureTestData.RouteIdKey)
                && entry.Properties[ProxyFailureTestData.RouteIdKey]!.ToString() == ProxyFailureTestData.CoreRouteId);
        leakedEntries.Should().BeEmpty();
    }
}
