using System.Net;
using HealthyFac.Shared.Application.UseCases.Decorators;
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

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.TokenEndpoint.Forms.Should()
            .ContainSingle()
            .Which
            .Should()
            .Contain(expected: "grant_type=client_credentials")
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

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
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

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 2);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.SecondToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithFullJitterAfterJitterWindow_FetchesNewToken()
    {
        using var harness = new ServiceTokenHarness(ServiceTokenTestData.FullJitterFraction);
        using var first = await harness.CallAsync();
        harness.Time.Advance(ServiceTokenTestData.AfterFullJitterAdvance);

        using var second = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithFullJitterBeforeJitterWindow_ReusesCachedToken()
    {
        using var harness = new ServiceTokenHarness(ServiceTokenTestData.FullJitterFraction);
        using var first = await harness.CallAsync();
        harness.Time.Advance(ServiceTokenTestData.BeforeFullJitterAdvance);

        using var second = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task ClientCredentialsHandler_ParallelRequests_FetchOnce()
    {
        using var harness = new ServiceTokenHarness();
        var calls = await harness.StartParallelCallsAsync(ServiceTokenTestData.ParallelCallCount);
        await harness.Time.WaitForTimersAsync(ServiceTokenTestData.ParallelCallCount);
        harness.TokenEndpoint.ReleaseResponses();
        await harness.Storage.TokenStored;
        harness.Time.Advance(ServiceTokenTestData.PollAdvance);

        var responses = await Task.WhenAll(calls);

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
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

    [Fact]
    public async Task ClientCredentialsHandler_WhenTokenEndpointRejectsClient_PutsOAuthErrorInMessage()
    {
        using var harness = new ServiceTokenHarness();
        harness.TokenEndpoint.StatusCode = HttpStatusCode.Unauthorized;
        harness.TokenEndpoint.ErrorBody = ServiceTokenTestData.InvalidClientBody;

        var act = harness.CallAsync;

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Which.Message.Should()
            .Contain(ServiceTokenTestData.InvalidClientDescription)
            .And.NotContain(ServiceTokenTestData.ClientSecret)
            .And.NotContain(unexpected: "grant_type");
    }

    [Fact]
    public async Task ClientCredentialsHandler_InUseCaseWithDownstreamScope_RequestsServiceScope()
    {
        using var harness = new ServiceTokenHarness();
        var decorator = new DownstreamScopeRequestHandlerDecorator<DownstreamScopeCallRequest, string>(
            new DownstreamScopeCallHandler(harness),
            harness.DownstreamScopes);

        await decorator.HandleAsync(new DownstreamScopeCallRequest());

        harness.ReadScopes().Should().Be(ServiceTokenTestData.FirstPrefixedScope);
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithTwoDownstreamScopes_RequestsBothScopes()
    {
        using var harness = new ServiceTokenHarness();
        using var downstreamScope = harness.DownstreamScopes.Enter(
            ServiceTokenTestData.SecondDownstreamScope,
            ServiceTokenTestData.FirstDownstreamScope);

        using var response = await harness.CallAsync();

        harness.ReadScopes()
            .Should()
            .Be($"{ServiceTokenTestData.FirstPrefixedScope} {ServiceTokenTestData.SecondPrefixedScope}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithSameDownstreamScopesInOtherOrder_ReusesToken()
    {
        using var harness = new ServiceTokenHarness();
        using (harness.DownstreamScopes.Enter(
            ServiceTokenTestData.FirstDownstreamScope,
            ServiceTokenTestData.SecondDownstreamScope))
        {
            using var first = await harness.CallAsync();
        }

        using (harness.DownstreamScopes.Enter(
            ServiceTokenTestData.SecondDownstreamScope,
            ServiceTokenTestData.FirstDownstreamScope))
        {
            using var second = await harness.CallAsync();
        }

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithDifferentDownstreamScopes_FetchesSeparateTokens()
    {
        using var harness = new ServiceTokenHarness();
        using (harness.DownstreamScopes.Enter(ServiceTokenTestData.FirstDownstreamScope))
        {
            using var first = await harness.CallAsync();
        }

        using (harness.DownstreamScopes.Enter(ServiceTokenTestData.SecondDownstreamScope))
        {
            using var second = await harness.CallAsync();
        }

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 2);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.SecondToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WithoutDownstreamScope_RequestsNoScope()
    {
        using var harness = new ServiceTokenHarness();

        using var response = await harness.CallAsync();

        harness.ReadScopes().Should().BeNull();
    }

    [Fact]
    public async Task ClientCredentialsHandler_InWorkerScopeWithDownstreamScope_RequestsScope()
    {
        using var harness = new ServiceTokenHarness();
        using var downstreamScope = harness.DownstreamScopes.Enter(ServiceTokenTestData.FirstDownstreamScope);

        using var response = await harness.CallAsync();

        harness.ReadScopes().Should().Be(ServiceTokenTestData.FirstPrefixedScope);
    }

    [Fact]
    public async Task ClientCredentialsHandler_TokenStoredByOtherInstance_ReusesIt()
    {
        using var harness = new ServiceTokenHarness();
        var otherInstance = harness.CreateClient();
        using var first = await harness.CallAsync();

        using var second = await otherInstance.GetAsync(ServiceTokenTestData.ResourceUrl);

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenStorageReadFails_FetchesFromTokenEndpoint()
    {
        using var harness = new ServiceTokenHarness();
        harness.Storage.IsReadFailing = true;

        using var response = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenStorageWriteFails_ReturnsFetchedToken()
    {
        using var harness = new ServiceTokenHarness();
        harness.Storage.IsWriteFailing = true;

        using var response = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
        harness.Storage.StoredKeys.Should().BeEmpty();
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenOtherInstanceHoldsLock_WaitsAndReusesItsToken()
    {
        using var harness = new ServiceTokenHarness();
        harness.Storage.HoldLock(ServiceTokenTestData.LockKey, ServiceTokenTestData.OtherInstanceOwner);
        var call = harness.CallAsync();
        await harness.Time.WaitForTimersAsync(count: 1);
        harness.Storage.StoreToken(
            ServiceTokenTestData.TokenKey,
            ServiceTokenTestData.OtherInstanceToken,
            harness.Time.GetUtcNow() + ServiceTokenTestData.NearExpiryAdvance);
        harness.Time.Advance(ServiceTokenTestData.PollAdvance);

        using var response = await call;

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 0);
        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.OtherInstanceToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenLockHolderNeverWrites_FetchesAfterLockTimeout()
    {
        using var harness = new ServiceTokenHarness();
        harness.Storage.HoldLock(ServiceTokenTestData.LockKey, ServiceTokenTestData.OtherInstanceOwner);
        var call = harness.CallAsync();
        await harness.Time.WaitForTimersAsync(count: 1);
        harness.Time.Advance(ServiceTokenTestData.AfterLockTimeoutAdvance);

        using var response = await call;

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
        harness.Storage.HeldLockKeys.Should().Equal(ServiceTokenTestData.LockKey);
    }

    [Fact]
    public async Task ClientCredentialsHandler_WhenLockCallFails_FetchesLocally()
    {
        using var harness = new ServiceTokenHarness();
        harness.Storage.IsLockFailing = true;

        using var response = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
        harness.Storage.StoredKeys.Should().Equal(ServiceTokenTestData.TokenKey);
    }

    [Fact]
    public async Task ClientCredentialsHandler_TwoInstancesAtOnce_FetchOnce()
    {
        using var harness = new ServiceTokenHarness();
        var otherInstance = harness.CreateClient();
        harness.TokenEndpoint.HoldResponses();
        var firstCall = harness.CallAsync();
        var secondCall = otherInstance.GetAsync(ServiceTokenTestData.ResourceUrl);
        await harness.Time.WaitForTimersAsync(count: 2);
        harness.TokenEndpoint.ReleaseResponses();
        using var first = await firstCall;
        harness.Time.Advance(ServiceTokenTestData.PollAdvance);

        using var second = await secondCall;

        harness.TokenEndpoint.RequestCount.Should().Be(expected: 1);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task ClientCredentialsHandler_AfterFetch_ReleasesLock()
    {
        using var harness = new ServiceTokenHarness();

        using var response = await harness.CallAsync();

        harness.Storage.LockAcquireCount.Should().Be(expected: 1);
        harness.Storage.HeldLockKeys.Should().BeEmpty();
    }
}
