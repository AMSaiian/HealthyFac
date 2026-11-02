using System.Net;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange;

public sealed class TokenExchangeTests(TokenExchangeFixture fixture) : IClassFixture<TokenExchangeFixture>
{
    private readonly TokenExchangeFixture _fixture = fixture;

    [Fact]
    public async Task Exchange_WithTargetAudience_ReturnsTokenForTargetOnly()
    {
        var subjectToken = await _fixture.Realm.GetSupervisorAccessTokenAsync();

        var response = await _fixture.ExchangeAsync(
            subjectToken,
            TokenExchangeTestData.TargetClientId,
            TokenExchangeTestData.ExchangeScopes);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = response.AccessTokenClaims();
        claims.Audiences().Should().Equal(TokenExchangeTestData.TargetClientId);
        claims[TokenExchangeTestData.SubjectClaim]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.SupervisorIdentityId.ToString());
        claims[TokenExchangeTestData.OrganizationClaim]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .Equal(RealmTestData.CompanyId.ToString());
        claims[RealmTestData.EmployeeIdAttribute]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.SupervisorEmployeeId.ToString());
        claims[RealmTestData.PermissionsClaim]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .BeEquivalentTo(RealmTestData.ExpectedPermissions);
    }

    [Fact]
    public async Task Exchange_ThroughInternalAddress_HasPublicIssuer()
    {
        var subjectToken = await _fixture.Realm.GetSupervisorAccessTokenAsync();

        var response = await _fixture.ExchangeAsync(
            subjectToken,
            TokenExchangeTestData.TargetClientId,
            TokenExchangeTestData.ExchangeScopes,
            RealmTestData.InternalHost);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.AccessTokenClaims()[TokenExchangeTestData.IssuerClaim]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.PublicIssuer);
    }

    [Fact]
    public async Task Exchange_WithAudienceNotAllowed_Fails()
    {
        var subjectToken = await _fixture.Realm.GetSupervisorAccessTokenAsync();

        var response = await _fixture.ExchangeAsync(
            subjectToken,
            TokenExchangeTestData.NotAllowedAudience,
            TokenExchangeTestData.ExchangeScopes);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Exchange_ForDisabledUser_Fails()
    {
        var subjectToken = await _fixture.Realm.GetSupervisorAccessTokenAsync();
        await _fixture.Realm.SetUserEnabledAsync(RealmTestData.SupervisorIdentityId, false);

        var response = await _fixture.ExchangeAsync(
            subjectToken,
            TokenExchangeTestData.TargetClientId,
            TokenExchangeTestData.ExchangeScopes);

        await _fixture.Realm.SetUserEnabledAsync(RealmTestData.SupervisorIdentityId, true);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Body[TokenExchangeTestData.ErrorField]!.GetValue<string>()
            .Should()
            .Be(TokenExchangeTestData.InvalidTokenError);
    }
}
