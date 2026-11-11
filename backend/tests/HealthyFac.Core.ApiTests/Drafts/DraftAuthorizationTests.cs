using System.Net;
using System.Text;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Drafts;

[Collection(CoreApiCollection.Name)]
public sealed class DraftAuthorizationTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DraftAuthorizationTestData.Endpoints), MemberType = typeof(DraftAuthorizationTestData))]
    public async Task DraftEndpoint_WithoutToken_Returns401(string method, string path, string? body)
    {
        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, mediaType: "application/json");
        }

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(DraftAuthorizationTestData.Endpoints), MemberType = typeof(DraftAuthorizationTestData))]
    public async Task DraftEndpoint_WithoutAnyDraftPermission_Returns403(string method, string path, string? body)
    {
        var claims = _fixture.CreateMachineClaims(
            DraftApiTestData.MachineReadPermission, DraftApiTestData.MachineWritePermission);

        var response = await _fixture.SendMachineRequestAsync(new HttpMethod(method), path, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
