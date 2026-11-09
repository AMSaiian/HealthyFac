using System.Net;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class FileAuthorizationTests(DocumentApiFixture fixture)
{
    private readonly DocumentApiFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(FileAuthorizationTestData.UploadEndpoints), MemberType = typeof(FileAuthorizationTestData))]
    public async Task Upload_WithoutRequiredPermission_Returns403(string path, string otherPermission)
    {
        var claims = _fixture.CreateClaims(otherPermission);

        var response = await _fixture.SendMultipartAsync(await ResolveAsync(path), claims, [Pdf()]);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(FileAuthorizationTestData.UploadPaths), MemberType = typeof(FileAuthorizationTestData))]
    public async Task Upload_WithoutToken_Returns401(string path)
    {
        var response = await _fixture.SendMultipartAsync(await ResolveAsync(path), claims: null, [Pdf()]);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static MultipartFile Pdf() =>
        new(
            FileName: "a.pdf",
            UploadFileApiTestData.PdfType,
            new GeneratedStream(UploadFileApiTestData.PdfHeader, length: 100));

    private async Task<string> ResolveAsync(string path) =>
        path == FileAuthorizationTestData.MachinePlaceholderPath
            ? UploadFileApiTestData.MachineFilesPath(await _fixture.ArrangeMachineAsync())
            : path;
}
