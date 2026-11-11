using System.Net;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class SearchFilesApiTests(DocumentApiFixture fixture)
{
    private const string SearchBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
    private const string ReadPermission = "files:read";

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task SearchCompanyFiles_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post, $"{UploadFileApiTestData.CompanyFilesPath}/search", claims, SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchMachineFiles_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);
        var machineId = await _fixture.ArrangeMachineAsync();

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/search",
            claims,
            SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithReadPermission_Returns200()
    {
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post, $"{UploadFileApiTestData.CompanyFilesPath}/search", claims, SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchCompanyFiles_WithEncryptedMachineIdInFilter_FindsTheTiedFile()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var tied = await _fixture.ArrangeFileAsync(machineId);
        var untied = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission);
        var body = $"{{\"offset\":0,\"limit\":25,\"filter\":{{\"machine\":{{\"machineIds\":[\"{machineId}\"]}}}}}}";

        var response = await _fixture.SendAsync(
            HttpMethod.Post, $"{UploadFileApiTestData.CompanyFilesPath}/search", claims, body);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain(tied.EncryptedId);
        content.Should().NotContain(untied.EncryptedId);
    }
}
