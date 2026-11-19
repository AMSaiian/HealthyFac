using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class GetFileApiTests(DocumentApiFixture fixture)
{
    private const string ReadPermission = "files:read";
    private const string MalformedId = "not-an-encrypted-id";

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task GetCompanyFile_WithReadPermission_Returns200WithTheRow()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}", claims);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "id"]!.GetValue<string>().Should().Be(file.EncryptedId);
        body[propertyName: "fileName"]!.GetValue<string>().Should().Be(expected: "manual");
        body[propertyName: "rowVersion"]!.GetValue<uint>().Should().Be(file.RowVersion);
    }

    [Fact]
    public async Task GetMachineFile_WithReadPermission_Returns200()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}", claims);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body[propertyName: "id"]!.GetValue<string>().Should().Be(file.EncryptedId);
        body[propertyName: "machineId"]!.GetValue<string>().Should().Be(machineId);
    }

    [Fact]
    public async Task GetFile_WithMalformedId_Returns404()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var claims = _fixture.CreateClaims(ReadPermission);

        var companyResponse = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.CompanyFilesPath}/{MalformedId}", claims);
        var machineResponse = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{MalformedId}", claims);

        companyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        machineResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFile_WithoutReadPermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var companyResponse = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}", claims);
        var machineResponse = await _fixture.SendAsync(
            HttpMethod.Get, $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}", claims);

        companyResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        machineResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFile_WithoutToken_Returns401()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        using var client = _fixture.CreateClient();

        using var companyResponse = await client.GetAsync(
            $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}", TestContext.Current.CancellationToken);
        using var machineResponse = await client.GetAsync(
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}",
            TestContext.Current.CancellationToken);

        companyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        machineResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
