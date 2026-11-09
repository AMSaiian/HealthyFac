using System.Net;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class GetFileLinksTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetFileLinks_WithFilesInScope_ReturnsOneLinkForEachAndLeavesOutTheRest()
    {
        var machineId = await _fixture.RegisterMachineAsync(GetFileLinksTestData.CompanyId);
        var uploadedAt = GetFileLinksTestData.UploadedAt;
        var ids = await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [
                new FileSeed(FileName: "company", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(
                    FileName: "embedded", Extension: "png", SizeBytes: 10, uploadedAt, Purpose: FilePurpose.Embedded),
                new FileSeed(FileName: "machine", Extension: "pdf", SizeBytes: 10, uploadedAt, machineId),
                new FileSeed(FileName: "deleted", Extension: "pdf", SizeBytes: 10, uploadedAt),
                new FileSeed(
                    FileName: "foreign", Extension: "pdf", SizeBytes: 10, uploadedAt,
                    CompanyId: GetFileLinksTestData.OtherCompanyId),
            ]);
        await _fixture.DeleteFileRefAsync(GetFileLinksTestData.CompanyId, ids[index: 3]);
        var requested = ids.Select(_fixture.Encrypt)
            .Concat([GetFileLinksTestData.MalformedId, _fixture.Encrypt(Guid.CreateVersion7())])
            .Append(_fixture.Encrypt(ids[index: 0]));

        var response = await _fixture.GetFileLinksAsync(
            GetFileLinksTestData.CompanyId, requested);

        response.Links.Select(link => link.Id).Should().BeEquivalentTo([ids[index: 0], ids[index: 1], ids[index: 2]]);
    }

    [Fact]
    public async Task GetFileLinks_WithPdf_BuildsTheUrlAndTheSasQuery()
    {
        var ids = await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [
                new FileSeed(FileName: "Pump manual", Extension: "pdf", SizeBytes: 10, GetFileLinksTestData.UploadedAt),
                new FileSeed(FileName: "scheme", Extension: "png", SizeBytes: 10, GetFileLinksTestData.UploadedAt),
            ]);
        var issuer = _fixture.CreateIssuer(GetFileLinksTestData.FixedNow, Environments.Production);
        var pdf = issuer.Issue(await _fixture.LoadFileRowAsync(ids[index: 0]));
        var png = issuer.Issue(await _fixture.LoadFileRowAsync(ids[index: 1]));
        var query = pdf.Query;

        pdf.Url.Should()
            .StartWith($"{GetFileLinksTestData.PublicBaseUrl}/{DatabaseTestData.ContainerName}/{ids[index: 0]}?");
        query[name: "sp"].Should().Be(expected: "r");
        query[name: "sr"].Should().Be(expected: "b");
        query[name: "spr"].Should().Be(expected: "https");
        query[name: "st"]
            .Should()
            .Be(
                GetFileLinksTestData.FixedNow.AddMinutes(minutes: -1).ToString(GetFileLinksTestData.SasTimeFormat));
        query[name: "se"]
            .Should()
            .Be(
                GetFileLinksTestData.FixedNow.AddSeconds(GetFileLinksTestData.LifetimeSeconds)
                    .ToString(GetFileLinksTestData.SasTimeFormat));
        query[name: "rsct"].Should().Be(expected: "application/pdf");
        query[name: "rscd"].Should().StartWith(expected: "attachment; filename=");
        png.Query[name: "rscd"].Should().Be(expected: "inline");
        pdf.ExpiresAt.Should()
            .Be(GetFileLinksTestData.FixedNow.AddSeconds(GetFileLinksTestData.LifetimeSeconds).UtcDateTime);
    }

    [Fact]
    public async Task GetFileLinks_InDevelopment_AllowsHttpAndHttps()
    {
        var ids = await _fixture.SeedFilesAsync(
            GetFileLinksTestData.CompanyId,
            [new FileSeed(FileName: "a", Extension: "pdf", SizeBytes: 10, GetFileLinksTestData.UploadedAt)]);
        var issuer = _fixture.CreateIssuer(GetFileLinksTestData.FixedNow, Environments.Development);

        var link = issuer.Issue(await _fixture.LoadFileRowAsync(ids[index: 0]));

        link.Query[name: "spr"].Should().Be(expected: "https,http");
    }

    [Fact]
    public async Task GetFileLinks_WithValidLink_ReadsTheBytesFromTheStorage()
    {
        var content = UploadFileTestData.Pdf;
        var response = await _fixture.UploadCompanyFileAsync(
            GetFileLinksTestData.CompanyId,
            UploadFileTestData.CreateUpload(UploadFileTestData.PdfName, content, UploadFileTestData.PdfType),
            cancellationToken: TestContext.Current.CancellationToken);
        var issuer = _fixture.CreateIssuer(DateTimeOffset.UtcNow, Environments.Development);
        var link = issuer.Issue(await _fixture.LoadFileRowAsync(response.Id));
        using var client = new HttpClient();

        using var read = await client.GetAsync(_fixture.RebaseOnStorage(link), TestContext.Current.CancellationToken);
        var bytes = await read.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Content.Headers.ContentType!.MediaType.Should().Be(UploadFileTestData.PdfType);
        read.Content.Headers.ContentDisposition!.DispositionType.Should().Be(expected: "attachment");
        bytes.Should().Equal(content);
    }

    [Fact]
    public async Task GetFileLinks_WithExpiredLink_IsRefusedByTheStorage()
    {
        var response = await _fixture.UploadCompanyFileAsync(
            GetFileLinksTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
            cancellationToken: TestContext.Current.CancellationToken);
        var issuer = _fixture.CreateIssuer(DateTimeOffset.UtcNow.AddHours(hours: -1), Environments.Development);
        var link = issuer.Issue(await _fixture.LoadFileRowAsync(response.Id));
        using var client = new HttpClient();

        using var read = await client.GetAsync(_fixture.RebaseOnStorage(link), TestContext.Current.CancellationToken);

        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetFileLinks_WithChangedSignature_IsRefusedByTheStorage()
    {
        var response = await _fixture.UploadCompanyFileAsync(
            GetFileLinksTestData.CompanyId,
            UploadFileTestData.CreateUpload(
                UploadFileTestData.PdfName, UploadFileTestData.Pdf, UploadFileTestData.PdfType),
            cancellationToken: TestContext.Current.CancellationToken);
        var issuer = _fixture.CreateIssuer(DateTimeOffset.UtcNow, Environments.Development);
        var link = issuer.Issue(await _fixture.LoadFileRowAsync(response.Id));
        var tampered = new Uri(_fixture.RebaseOnStorage(link).AbsoluteUri.Replace(oldValue: "sig=", newValue: "sig=A"));
        using var client = new HttpClient();

        using var read = await client.GetAsync(tampered, TestContext.Current.CancellationToken);

        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public void Validate_WithIdCountAtAndAboveTheLimit_AcceptsOnlyTheLimit()
    {
        var validator = new GetFileLinksRequestValidator();

        var atLimit = validator.Validate(
            new GetFileLinksRequest
                { CompanyId = GetFileLinksTestData.CompanyId, Ids = GetFileLinksTestData.IdsAtTheLimit });
        var above = validator.Validate(
            new GetFileLinksRequest
            {
                CompanyId = GetFileLinksTestData.CompanyId,
                Ids = GetFileLinksTestData.IdsAboveTheLimit,
            });

        atLimit.IsValid.Should().BeTrue();
        above.IsValid.Should().BeFalse();
    }
}
