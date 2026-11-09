using HealthyFac.Document.Application.Files.Storage;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Document.IntegrationTests.Database;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Document.IntegrationTests.Files.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Files;

[Collection(DocumentDatabaseCollection.Name)]
public sealed class BlobFileStorageTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task WriteAsync_NewId_StoresBytesAndHeaders()
    {
        var id = Guid.CreateVersion7();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        await storage.WriteAsync(
            id,
            FilePurpose.Attachment,
            new MemoryStream(BlobFileStorageTestData.Content),
            BlobFileStorageTestData.ContentType,
            BlobFileStorageTestData.ContentDisposition,
            TestContext.Current.CancellationToken);

        var blob = _fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
            .GetBlobClient(id.ToString());
        var properties = await blob.GetPropertiesAsync(cancellationToken: TestContext.Current.CancellationToken);
        var download = await blob.DownloadContentAsync(TestContext.Current.CancellationToken);

        properties.Value.ContentType.Should().Be(BlobFileStorageTestData.ContentType);
        properties.Value.ContentDisposition.Should().Be(BlobFileStorageTestData.ContentDisposition);
        download.Value.Content.ToArray().Should().Equal(BlobFileStorageTestData.Content);
    }

    [Fact]
    public async Task WriteAsync_ExistingId_Throws()
    {
        var id = Guid.CreateVersion7();
        using var scope = _fixture.ScopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        await storage.WriteAsync(
            id,
            FilePurpose.Attachment,
            new MemoryStream(BlobFileStorageTestData.Content),
            BlobFileStorageTestData.ContentType,
            BlobFileStorageTestData.ContentDisposition,
            TestContext.Current.CancellationToken);

        var act = () => storage.WriteAsync(
            id,
            FilePurpose.Attachment,
            new MemoryStream([9, 9]),
            BlobFileStorageTestData.ContentType,
            BlobFileStorageTestData.ContentDisposition,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>();

        var download = await _fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
            .GetBlobClient(id.ToString())
            .DownloadContentAsync(TestContext.Current.CancellationToken);

        download.Value.Content.ToArray().Should().Equal(BlobFileStorageTestData.Content);
    }

    [Fact]
    public async Task EnsureContainerAsync_RunTwice_Succeeds()
    {
        var storage = _fixture.GetSingleton<BlobFileStorage>();
        await storage.EnsureContainerAsync(TestContext.Current.CancellationToken);

        var act = () => storage.EnsureContainerAsync(TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
