using System.Net;
using System.Text.Json;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Testing.Features;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
[Feature(TestFeatures.Files)]
public sealed class SearchFilesApiTests(DocumentApiFixture fixture)
{
    private const string SearchBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
    private const string WithoutFilterBody = "{\"offset\":0,\"limit\":25}";
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

    [Fact]
    public async Task Search_WithoutFilter_ReturnsFirstPageOfAllRows()
    {
        var first = await _fixture.ArrangeFileAsync();
        var second = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(ReadPermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post, $"{UploadFileApiTestData.CompanyFilesPath}/search", claims, WithoutFilterBody);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        var ids = document.RootElement.GetProperty(propertyName: "items")
            .EnumerateArray()
            .Select(item => _fixture.IdEncryptor.Decrypt(item.GetProperty(propertyName: "id").GetString()!))
            .ToList();

        var totalCount = document.RootElement.GetProperty(propertyName: "totalCount").GetInt32();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ids.Should()
            .BeEquivalentTo(
                [_fixture.IdEncryptor.Decrypt(first.EncryptedId), _fixture.IdEncryptor.Decrypt(second.EncryptedId)]);

        totalCount.Should().Be(expected: 2);
    }
}
