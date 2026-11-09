using System.Net;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class UpdateDeleteFileApiTests(DocumentApiFixture fixture)
{
    private const string DeletePermission = "files:delete";
    private const string CompanyDeletePermission = "company-files:delete";

    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task UpdateCompanyFile_WithPermission_Returns204()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}",
            claims,
            UpdateBody(file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateCompanyFile_WithStaleRowVersion_Returns409()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);
        var path = $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}";
        await _fixture.SendAsync(HttpMethod.Put, path, claims, UpdateBody(file.RowVersion));

        var response = await _fixture.SendAsync(HttpMethod.Put, path, claims, UpdateBody(file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCompanyFile_WithMalformedId_Returns404()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{UploadFileApiTestData.CompanyFilesPath}/not-an-encrypted-id",
            claims,
            UpdateBody(rowVersion: 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMachineFile_WithPermission_Returns204()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}",
            claims,
            UpdateBody(file.RowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCompanyFile_WithPermission_Returns204()
    {
        var file = await _fixture.ArrangeFileAsync();
        var claims = _fixture.CreateClaims(CompanyDeletePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete, $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}", claims);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteMachineFile_WithPermission_Returns204()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var claims = _fixture.CreateClaims(DeletePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Delete,
            $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateAndDelete_WithWrongPermission_Return403()
    {
        var machineId = await _fixture.ArrangeMachineAsync();
        var file = await _fixture.ArrangeFileAsync(machineId);
        var readOnly = _fixture.CreateClaims("files:read");
        var companyPath = $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}";
        var machinePath = $"{UploadFileApiTestData.MachineFilesPath(machineId)}/{file.EncryptedId}";

        var responses = new[]
        {
            await _fixture.SendAsync(HttpMethod.Put, companyPath, readOnly, UpdateBody(file.RowVersion)),
            await _fixture.SendAsync(HttpMethod.Delete, companyPath, readOnly),
            await _fixture.SendAsync(HttpMethod.Put, machinePath, readOnly, UpdateBody(file.RowVersion)),
            await _fixture.SendAsync(HttpMethod.Delete, machinePath, readOnly),
        };

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAndDelete_WithoutToken_Return401()
    {
        var file = await _fixture.ArrangeFileAsync();
        var path = $"{UploadFileApiTestData.CompanyFilesPath}/{file.EncryptedId}";
        using var client = _fixture.CreateClient();

        using var update = await client.PutAsync(
            path, new StringContent(UpdateBody(file.RowVersion)), TestContext.Current.CancellationToken);
        using var delete = await client.DeleteAsync(path, TestContext.Current.CancellationToken);

        update.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string UpdateBody(uint rowVersion) =>
        $"{{\"fileName\":\"renamed\",\"description\":null,\"rowVersion\":{rowVersion}}}";
}
