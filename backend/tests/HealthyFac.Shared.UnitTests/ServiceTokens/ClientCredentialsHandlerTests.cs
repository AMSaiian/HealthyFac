using System.Net;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class ClientCredentialsHandlerTests
{
    private const string BearerPrefix = "Bearer ";

    [Fact]
    public async Task ClientCredentialsHandler_FirstRequest_FetchesAndAttachesToken()
    {
        using var harness = new ServiceTokenHarness();

        using var response = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(1);
        harness.TokenEndpoint.Forms.Should()
            .ContainSingle()
            .Which
            .Should()
            .Contain("grant_type=client_credentials")
            .And.Contain($"client_id={ServiceTokenTestData.ClientId}")
            .And.Contain($"client_secret={ServiceTokenTestData.ClientSecret}");

        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_SecondRequest_ReusesCachedToken()
    {
        using var harness = new ServiceTokenHarness();
        using var first = await harness.CallAsync();
        harness.Time.Advance(ServiceTokenTestData.ReuseAdvance);

        using var second = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(1);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_NearExpiry_FetchesNewToken()
    {
        using var harness = new ServiceTokenHarness();
        using var first = await harness.CallAsync();
        harness.Time.Advance(ServiceTokenTestData.NearExpiryAdvance);

        using var second = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(2);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.SecondToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_ParallelRequests_FetchOnce()
    {
        using var harness = new ServiceTokenHarness();
        var calls = await harness.StartParallelCallsAsync(ServiceTokenTestData.ParallelCallCount);
        harness.TokenEndpoint.ReleaseResponses();

        var responses = await Task.WhenAll(calls);

        harness.TokenEndpoint.RequestCount.Should().Be(1);
        harness.Resource.Authorizations.Should()
            .HaveCount(ServiceTokenTestData.ParallelCallCount)
            .And.OnlyContain(authorization => authorization == $"{BearerPrefix}{ServiceTokenTestData.FirstToken}");

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenTokenEndpointFails_ThrowsAndSendsNothing()
    {
        using var harness = new ServiceTokenHarness();
        harness.TokenEndpoint.StatusCode = HttpStatusCode.InternalServerError;

        var act = harness.CallAsync;

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        harness.Resource.Authorizations.Should().BeEmpty();
    }
}
