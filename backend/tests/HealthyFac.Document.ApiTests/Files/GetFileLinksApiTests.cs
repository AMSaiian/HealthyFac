using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class GetFileLinksApiTests(DocumentApiFixture fixture)
{
    private const string ReadPermission = "files:read";
    private const string LinksPath = "v1/files/links";
    private const int TooManyIds = 1001;

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task GetFileLinks_WithReadPermission_Returns200WithTheLink()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(HttpMethod.Post, LinksPath, claims, IdsBody(file.EncryptedId));
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "links"]!.AsArray().Should().ContainSingle();
        body[propertyName: "links"]![index: 0]![propertyName: "id"]!.GetValue<string>().Should().Be(file.EncryptedId);
    }

    [Fact]
    public async Task GetFileLinks_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Post, LinksPath, claims, IdsBody("id"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFileLinks_WithTooManyIds_Returns400()
    {
        var claims = _fixture.CreateClaims(ReadPermission);
        var body = IdsBody([.. Enumerable.Range(start: 0, TooManyIds).Select(_ => "id")]);

        var response = await _fixture.SendAsync(HttpMethod.Post, LinksPath, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string IdsBody(params string[] ids) =>
        new JsonObject { [propertyName: "ids"] = new JsonArray([.. ids.Select(id => (JsonNode)id)]) }.ToJsonString();
}
