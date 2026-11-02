using System.Net;
using System.Web;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class TokenExchangeHandlerTests
{
    private const string BearerPrefix = "Bearer ";

    [Fact]
    public async Task UserDelegatedHandler_WithIncomingToken_AttachesExchangedToken()
    {
        using var harness = new TokenExchangeHarness();

        using var response = await harness.CallAsync();

        harness.Resource.Authorizations.Should().Equal($"{BearerPrefix}{ServiceTokenTestData.FirstToken}");

        var form = HttpUtility.ParseQueryString(harness.TokenEndpoint.Forms.Single());
        form[ServiceTokenConstants.Form.GrantType].Should().Be(ServiceTokenConstants.Form.TokenExchangeGrant);
        form[ServiceTokenConstants.Form.SubjectToken].Should().Be(ServiceTokenTestData.FirstSubjectToken);
        form[ServiceTokenConstants.Form.SubjectTokenType].Should().Be(ServiceTokenConstants.Form.AccessTokenType);
        form[ServiceTokenConstants.Form.Audience].Should().Be(ServiceTokenTestData.TargetAudience);
        form[ServiceTokenConstants.Form.ClientId].Should().Be(ServiceTokenTestData.ClientId);
        form[ServiceTokenConstants.Form.Scope].Should().Be(ServiceTokenTestData.ExpectedExchangeScope);
    }

    [Fact]
    public async Task UserDelegatedHandler_SameUserTwice_ExchangesOnce()
    {
        using var harness = new TokenExchangeHarness();
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
    public async Task UserDelegatedHandler_NearExpiry_ExchangesAgain()
    {
        using var harness = new TokenExchangeHarness();
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
    public async Task UserDelegatedHandler_TwoUsers_UseSeparateTokens()
    {
        using var harness = new TokenExchangeHarness();
        using var firstUserCall = await harness.CallAsync();
        harness.SignInAs(ServiceTokenTestData.SecondUserId, ServiceTokenTestData.SecondSubjectToken);
        using var secondUserCall = await harness.CallAsync();
        harness.SignInAs(ServiceTokenTestData.FirstUserId, ServiceTokenTestData.FirstSubjectToken);

        using var firstUserAgain = await harness.CallAsync();

        harness.TokenEndpoint.RequestCount.Should().Be(2);
        harness.Resource.Authorizations.Should()
            .Equal(
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}",
                $"{BearerPrefix}{ServiceTokenTestData.SecondToken}",
                $"{BearerPrefix}{ServiceTokenTestData.FirstToken}");
    }

    [Fact]
    public async Task UserDelegatedHandler_WithoutIncomingToken_Throws()
    {
        using var harness = new TokenExchangeHarness();
        harness.Incoming.AccessToken = null;
        harness.Incoming.UserId = null;

        var act = harness.CallAsync;

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(ServiceTokenErrors.NoIncomingUserToken().Message);

        harness.TokenEndpoint.RequestCount.Should().Be(0);
        harness.Resource.Authorizations.Should().BeEmpty();
    }

    [Fact]
    public async Task UserDelegatedHandler_WhenExchangeFails_ThrowsAndSendsNothing()
    {
        using var harness = new TokenExchangeHarness();
        harness.TokenEndpoint.StatusCode = HttpStatusCode.BadRequest;
        harness.TokenEndpoint.ErrorBody = ServiceTokenTestData.InvalidSubjectBody;

        var act = harness.CallAsync;

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should()
            .Contain(ServiceTokenTestData.InvalidSubjectDescription)
            .And.NotContain(ServiceTokenTestData.FirstSubjectToken)
            .And.NotContain(ServiceTokenTestData.ClientSecret);

        harness.Resource.Authorizations.Should().BeEmpty();
    }
}
