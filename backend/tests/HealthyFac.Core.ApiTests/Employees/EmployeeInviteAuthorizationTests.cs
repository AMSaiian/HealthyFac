using System.Net;
using System.Net.Mime;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Employees;

[Collection(CoreApiCollection.Name)]
public sealed class EmployeeInviteAuthorizationTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task CreateInvite_WithoutToken_Returns401()
    {
        var response = await _fixture.PostCreateInviteAsync(claims: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInvite_WithoutInvitePermission_Returns403()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = ["machines:write"] };

        var response = await _fixture.PostCreateInviteAsync(claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateInvite_WithOnlyInvitePermission_Returns201()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = ["employees:invite"] };

        var response = await _fixture.PostCreateInviteAsync(claims);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateInvite_WithWrongAudience_Returns401()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Audience = EmployeeInviteApiTestData.OtherAudience };

        var response = await _fixture.PostCreateInviteAsync(claims);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInvite_UsesCompanyFromToken()
    {
        var response = await _fixture.PostCreateInviteAsync(_fixture.CreateSupervisorClaims());
        var invite = await _fixture.LoadInviteAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        invite.CompanyId.Should().Be(_fixture.CompanyId);
    }

    [Fact]
    public async Task CreateInvite_WithBodyCompanyId_IgnoresIt()
    {
        var extraFields = new Dictionary<string, object?>
        {
            [key: "companyId"] = _fixture.IdEncryptor.Encrypt(EmployeeInviteApiTestData.OtherCompanyId),
        };

        var response = await _fixture.PostCreateInviteAsync(_fixture.CreateSupervisorClaims(), extraFields);
        var invite = await _fixture.LoadInviteAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        invite.CompanyId.Should().Be(_fixture.CompanyId);
    }

    [Fact]
    public async Task Endpoint_WithServiceTokenWithPermission_PassesPolicyAndFailsDepartmentScope()
    {
        var claims = new TestServiceTokenClaims { Permissions = ["employees:invite"] };

        var response = await _fixture.PostCreateInviteAsServiceAsync(
            claims,
            EmployeeInviteApiTestData.ServiceCallerUserId,
            _fixture.CompanyId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be(MediaTypeNames.Application.Json);
    }

    [Fact]
    public async Task Endpoint_WithServiceTokenWithoutPermission_Returns403()
    {
        var claims = new TestServiceTokenClaims { Permissions = ["machines:write"] };

        var response = await _fixture.PostCreateInviteAsServiceAsync(
            claims,
            EmployeeInviteApiTestData.ServiceCallerUserId,
            _fixture.CompanyId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyInvite_WithoutToken_IsAllowed()
    {
        var response = await _fixture.PostVerifyInviteAnonymouslyAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActorMiddleware_WithTwoOrganizations_Returns401()
    {
        var claims = _fixture.CreateSupervisorClaims() with
        {
            Organizations = [_fixture.CompanyId.ToString(), EmployeeInviteApiTestData.OtherCompanyId.ToString()],
        };

        var response = await _fixture.PostCreateInviteAsync(claims);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInvite_WithValidToken_AuditsWithTokenSubject()
    {
        var claims = _fixture.CreateSupervisorClaims();

        var response = await _fixture.PostCreateInviteAsync(claims);
        var invite = await _fixture.LoadInviteAsync(response);

        invite.CreatedBy.Should().Be(claims.Subject!.Value);
    }

    [Fact]
    public async Task Accept_AsAnonymous_AuditsAnonymousActor()
    {
        var createResponse = await _fixture.PostCreateInviteAsync(_fixture.CreateSupervisorClaims());
        var invite = await _fixture.LoadInviteAsync(createResponse);

        var response = await _fixture.PostAcceptInviteAnonymouslyAsync(invite);

        var employee = await _fixture.LoadEmployeeAsync(invite.Email);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        employee.CreatedBy.Should().Be(KnownActors.Anonymous.UserId);
    }
}
