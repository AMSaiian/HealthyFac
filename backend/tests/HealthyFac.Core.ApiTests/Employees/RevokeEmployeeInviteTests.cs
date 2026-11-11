using System.Net;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;

namespace HealthyFac.Core.ApiTests.Employees;

[Collection(CoreApiCollection.Name)]
public sealed class RevokeEmployeeInviteTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task RevokeInvite_WithoutToken_Returns401()
    {
        var invite = await CreateInviteAsync();

        var response = await _fixture.DeleteInviteAsync(claims: null, EncryptedIdOf(invite), rowVersion: 1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeInvite_WithoutInvitePermission_Returns403()
    {
        var invite = await CreateInviteAsync();
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = ["machines:write"] };

        var response = await _fixture.DeleteInviteAsync(claims, EncryptedIdOf(invite), rowVersion: 1);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeInvite_WithInvitePermissionAndCurrentRowVersion_Returns204()
    {
        var invite = await CreateInviteAsync();
        var rowVersion = await _fixture.LoadInviteRowVersionAsync(invite.Id);
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = ["employees:invite"] };

        var response = await _fixture.DeleteInviteAsync(claims, EncryptedIdOf(invite), rowVersion);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeInvite_WithMalformedId_Returns404()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = ["employees:invite"] };

        var response = await _fixture.DeleteInviteAsync(
            claims,
            EmployeeInviteApiTestData.NotAnEncryptedId,
            rowVersion: 1);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HealthyFac.Core.Domain.Employees.EmployeeInvite> CreateInviteAsync()
    {
        var response = await _fixture.PostCreateInviteAsync(_fixture.CreateSupervisorClaims());

        return await _fixture.LoadInviteAsync(response);
    }

    private string EncryptedIdOf(HealthyFac.Core.Domain.Employees.EmployeeInvite invite) =>
        _fixture.IdEncryptor.Encrypt(invite.Id);
}
