using System.Net;
using HealthyFac.Document.ApiTests.Files.Data;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Document.Application.Files.Inspection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Document.ApiTests.Files;

[Collection(DocumentApiCollection.Name)]
public sealed class UploadFileApiTests(DocumentApiFixture fixture)
{
    private readonly DocumentApiFixture _fixture = fixture;

    [Fact]
    public async Task UploadCompanyFile_WithPdf_Returns201AndStoresRef()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "manual.pdf", length: 1000)],
            description: "text");

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        refs.Should().ContainSingle();
        refs.Single().FileName.Should().Be(expected: "manual");
    }

    [Fact]
    public async Task UploadCompanyFile_WithCrLfAndOuterSpacesInDescription_StoresTheNormalizedText()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "manual.pdf", length: 100)],
            UploadFileApiTestData.CrLfDescription);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        refs.Single().Description.Should().Be(UploadFileApiTestData.NormalizedDescription);
    }

    [Fact]
    public async Task UploadCompanyFile_WithDecomposedFileName_StoresTheComposedBaseName()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(UploadFileApiTestData.DecomposedPdfName, length: 100)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        refs.Single().FileName.Should().Be(UploadFileApiTestData.ComposedBaseName);
    }

    [Fact]
    public async Task UploadCompanyFile_WithRenamedPng_Returns400FileTypeNotAllowed()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);
        var file = new MultipartFile(
            FileName: "scheme.pdf",
            UploadFileApiTestData.PdfType,
            new GeneratedStream(UploadFileApiTestData.PngHeader, length: 1000));

        var response = await _fixture.SendMultipartAsync(UploadFileApiTestData.CompanyFilesPath, claims, [file]);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain(UploadFileApiTestData.FileTypeNotAllowed);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithExactlyTheAttachmentLimit_Returns201()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "big.pdf", UploadFileApiTestData.AttachmentMaxBytes)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        refs.Single().SizeBytes.Should().Be(UploadFileApiTestData.AttachmentMaxBytes);
    }

    [Fact]
    public async Task UploadCompanyFile_AboveTheAttachmentLimit_Returns400AndStoresNothing()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "big.pdf", UploadFileApiTestData.AttachmentMaxBytes + 1)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadEmbeddedImage_WithExactlyTheEmbeddedLimit_Returns201()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.EmbeddedPath, claims, [Png(UploadFileApiTestData.EmbeddedMaxBytes)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UploadEmbeddedImage_AboveTheEmbeddedLimit_Returns400AndStoresNothing()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.EmbeddedPath, claims, [Png(UploadFileApiTestData.EmbeddedMaxBytes + 1)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithEmptyFile_Returns400()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath, claims, [Pdf(fileName: "empty.pdf", length: 0)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCompanyFile_WithoutFilePart_Returns400()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath, claims, [], description: "no file");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCompanyFile_WithTwoFileParts_Returns400()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "a.pdf", length: 100), Pdf(fileName: "b.pdf", length: 100)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadMachineFile_WithMachineInScope_Returns201()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);
        var machineId = await _fixture.ArrangeMachineAsync();

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.MachineFilesPath(machineId), claims, [Pdf(fileName: "m.pdf", length: 100)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        refs.Single().MachineId.Should().Be(_fixture.IdEncryptor.Decrypt(machineId));
    }

    [Fact]
    public async Task UploadMachineFile_WithForeignMachine_Returns404()
    {
        var claims = _fixture.CreateClaims(UploadFileApiTestData.FilesWritePermission);
        var machineId = await _fixture.ArrangeMachineAsync(Guid.CreateVersion7());

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.MachineFilesPath(machineId), claims, [Pdf(fileName: "m.pdf", length: 100)]);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadCompanyFile_WithStorageDown_Returns503AndStoresNoRef()
    {
        await using var factory = _fixture.CreateFactoryWithStorage(DocumentApiTestData.UnreachableStorageConnection);
        using var client = factory.CreateClient();
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);

        var response = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "down.pdf", length: 100)],
            client: client);

        var refs = await _fixture.LoadFileRefsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadConcurrency_WithFullQueue_Returns429()
    {
        var inspector = new GatedFileInspector();
        await using var factory = _fixture.CreateFactoryWithStorage(
            _fixture.StorageConnectionString,
            services => services.Replace(ServiceDescriptor.Singleton<IFileInspector>(inspector)));
        using var client = factory.CreateClient();
        var claims = _fixture.CreateClaims(UploadFileApiTestData.CompanyWritePermission);
        var accepted = Enumerable.Range(start: 0, UploadFileApiTestData.PermitLimit + UploadFileApiTestData.QueueLimit)
            .Select(_ => _fixture.SendMultipartAsync(
                UploadFileApiTestData.CompanyFilesPath,
                claims,
                [Pdf(fileName: "queued.pdf", length: 100)],
                client: client))
            .ToList();
        await WaitUntilAsync(() => inspector.EnteredCount == UploadFileApiTestData.PermitLimit);
        await Task.Delay(TimeSpan.FromSeconds(seconds: 1), TestContext.Current.CancellationToken);

        var rejected = await _fixture.SendMultipartAsync(
            UploadFileApiTestData.CompanyFilesPath,
            claims,
            [Pdf(fileName: "extra.pdf", length: 100)],
            client: client);

        inspector.Open();
        var responses = await Task.WhenAll(accepted);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.BadRequest);
    }

    private static MultipartFile Pdf(string fileName, long length) =>
        new(fileName, UploadFileApiTestData.PdfType, new GeneratedStream(UploadFileApiTestData.PdfHeader, length));

    private static MultipartFile Png(long length) =>
        new(
            FileName: "image.png", UploadFileApiTestData.PngType,
            new GeneratedStream(UploadFileApiTestData.PngHeader, length));

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(seconds: 30));

        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50), timeout.Token);
        }
    }
}
