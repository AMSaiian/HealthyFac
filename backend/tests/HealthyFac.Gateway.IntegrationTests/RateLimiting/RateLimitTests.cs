using System.Net;
using System.Net.Http.Json;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.RateLimiting.Data;
using HealthyFac.Gateway.Web.Proxy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Gateway.IntegrationTests.RateLimiting;

public sealed class RateLimitTests(GatewayEnvironment environment) : IClassFixture<GatewayEnvironment>, IAsyncLifetime
{
    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public Task InitializeAsync() => _core.StartAsync();

    public Task DisposeAsync() => _core.DisposeAsync().AsTask();

    [Fact]
    public async Task StrictPolicy_OverLimit_Returns429WithRetryAfter()
    {
        await using var gateway = environment.CreateGateway(_core, settings: RateLimitTestData.StrictLimitOfThree);
        using var client = gateway.CreateClient();
        await client.SpendStrictLimitAsync();

        using var response = await client.PostVerifyInviteAsync();

        var hasRetryAfter = response.Headers.TryGetValues("Retry-After", out var retryAfterValues);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        hasRetryAfter.Should().BeTrue();
        int.TryParse(retryAfterValues!.Single(), out var retryAfterSeconds).Should().BeTrue();
        retryAfterSeconds.Should().BeInRange(1, 60);
        _core.RequestCount.Should().Be(RateLimitTestData.StrictPermitLimit);
    }

    [Fact]
    public async Task StrictPolicy_OverLimit_ReturnsProblemDetails()
    {
        await using var gateway = environment.CreateGateway(_core, settings: RateLimitTestData.StrictLimitOfThree);
        using var client = gateway.CreateClient();
        await client.SpendStrictLimitAsync();

        using var response = await client.PostVerifyInviteAsync();

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        response.Content.Headers.ContentType!.MediaType.Should().Be(RateLimitTestData.ProblemJsonMediaType);
        problem!.Status.Should().Be(StatusCodes.Status429TooManyRequests);
        problem.Title.Should().Be(GatewayProblemDetailsConstants.TooManyRequests.Title);
    }

    [Fact]
    public async Task StrictPolicy_AcrossTwoGatewayInstances_SharesOneLimit()
    {
        var clientAddress = environment.NextClientAddress();
        await using var firstGateway = environment.CreateGateway(
            _core,
            clientAddress,
            RateLimitTestData.StrictLimitOfThree);
        await using var secondGateway = environment.CreateGateway(
            _core,
            clientAddress,
            RateLimitTestData.StrictLimitOfThree);
        using var firstClient = firstGateway.CreateClient();
        using var secondClient = secondGateway.CreateClient();
        using var firstResponse = await firstClient.PostVerifyInviteAsync();
        using var secondResponse = await firstClient.PostVerifyInviteAsync();
        using var thirdResponse = await secondClient.PostVerifyInviteAsync();

        using var response = await secondClient.PostVerifyInviteAsync();

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _core.RequestCount.Should().Be(RateLimitTestData.StrictPermitLimit);
    }

    [Fact]
    public async Task StrictPolicy_WithSpoofedForwardedFor_StillLimitsByConnectionAddress()
    {
        await using var gateway = environment.CreateGateway(_core, settings: RateLimitTestData.StrictLimitOfThree);
        using var client = gateway.CreateClient();
        using var firstResponse = await client.PostVerifyInviteAsync("198.51.100.1");
        using var secondResponse = await client.PostVerifyInviteAsync("198.51.100.2");
        using var thirdResponse = await client.PostVerifyInviteAsync("198.51.100.3");

        using var response = await client.PostVerifyInviteAsync("198.51.100.4");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        _core.RequestCount.Should().Be(RateLimitTestData.StrictPermitLimit);
    }

    [Fact]
    public async Task DefaultPolicy_UnderLimit_PassesOtherCoreRoutes()
    {
        await using var gateway = environment.CreateGateway(_core, settings: RateLimitTestData.StrictLimitOfThree);
        using var client = gateway.CreateAuthenticatedClient();
        await client.SpendStrictLimitAsync();

        using var response = await client.GetAsync(RateLimitTestData.MachinesRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
