using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.ApiTests.Employees;

public sealed class EmployeeInviteInputTests(CoreApiFixture fixture) : IClassFixture<CoreApiFixture>
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task CreateInvite_WithNullCharacterInEmail_Returns400WithoutTheValue()
    {
        var extraFields = new Dictionary<string, object?>
        {
            [EmployeeInviteApiTestData.EmailField] = EmployeeInviteApiTestData.NullCharacterEmail,
        };

        var response = await _fixture.PostCreateInviteAsync(_fixture.CreateSupervisorClaims(), extraFields);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        var text = body!.ToJsonString();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        text.Should().Contain(TextInputErrors.NullCharacter);
        text.Should().NotContain(EmployeeInviteApiTestData.NullCharacterEmailDomain);
    }

    /// <summary>
    /// Uses an accepted invite, because the employee check has no unique index behind it: only the normalized
    /// request email can find the employee.
    /// </summary>
    [Fact]
    public async Task CreateInvite_WithEmailOfEmployeeInOtherCase_ReturnsConflict()
    {
        var claims = _fixture.CreateSupervisorClaims();
        var created = await _fixture.PostCreateInviteAsync(
            claims,
            new Dictionary<string, object?>
            {
                [EmployeeInviteApiTestData.EmailField] = EmployeeInviteApiTestData.LowerCaseEmail,
            });
        var invite = await _fixture.LoadInviteAsync(created);
        var accepted = await _fixture.PostAcceptInviteAnonymouslyAsync(invite);
        var mixedCase = new Dictionary<string, object?>
        {
            [EmployeeInviteApiTestData.EmailField] = EmployeeInviteApiTestData.MixedCaseEmail,
        };

        var response = await _fixture.PostCreateInviteAsync(claims, mixedCase);

        var body = await response.Content.ReadFromJsonAsync<JsonObject>();

        accepted.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body!.ToJsonString().Should().Contain(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
    }
}
