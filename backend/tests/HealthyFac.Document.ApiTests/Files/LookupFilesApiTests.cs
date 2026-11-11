using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class LookupFilesApiTests(DocumentApiFixture fixture)
{
    private const string ReadPermission = "files:read";
    private const string LookupPath = "v1/files/lookup";
    private const string MalformedId = "not-an-encrypted-id";
    private const int TooManyIds = 101;

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task LookupFiles_WithReadPermission_Returns200WithTheFileAndLeavesOutBadIds()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            LookupPath,
            claims,
            IdsBody(file.EncryptedId, MalformedId));
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "files"]!.AsArray().Should().ContainSingle();
        body[propertyName: "files"]![index: 0]![propertyName: "id"]!.GetValue<string>().Should().Be(file.EncryptedId);
    }

    [Fact]
    public async Task LookupFiles_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendAsync(HttpMethod.Post, LookupPath, claims, IdsBody("id"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LookupFiles_With101Ids_Returns400()
    {
        var claims = _fixture.CreateClaims(ReadPermission);
        var body = IdsBody([.. Enumerable.Range(start: 0, TooManyIds).Select(_ => "id")]);

        var response = await _fixture.SendAsync(HttpMethod.Post, LookupPath, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string IdsBody(params string[] ids) =>
        new JsonObject { [propertyName: "ids"] = new JsonArray([.. ids.Select(id => (JsonNode)id)]) }.ToJsonString();
}
