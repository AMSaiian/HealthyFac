using HealthyFac.Document.Application;
using HealthyFac.Document.Application.Files;
using HealthyFac.Document.Application.Files.Searching;
using HealthyFac.Document.Application.Files.Uploading;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Persistence;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

/// <summary>
/// Runs the file use cases as a user and reads the stored refs and blobs. Every method opens its own DI scope, so it
/// reads what is stored, not tracked objects.
/// </summary>
public static class FileFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task<UploadFileResponse> UploadCompanyFileAsync(
            Guid companyId,
            FileUpload upload,
            string? description = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UploadFileRequest, UploadFileResponse>>();

            return await handler.HandleAsync(
                new UploadFileRequest { CompanyId = companyId, File = upload, Description = description },
                cancellationToken);
        }

        public async Task<UploadFileResponse> UploadMachineFileAsync(
            Guid companyId,
            Guid machineId,
            FileUpload upload
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UploadFileRequest, UploadFileResponse>>();

            return await handler.HandleAsync(
                new UploadFileRequest { CompanyId = companyId, MachineId = machineId, File = upload });
        }

        public async Task<UploadFileResponse> UploadEmbeddedImageAsync(
            Guid companyId,
            FileUpload upload
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UploadEmbeddedImageRequest, UploadFileResponse>>();

            return await handler.HandleAsync(new UploadEmbeddedImageRequest { CompanyId = companyId, File = upload });
        }

        public async Task<IReadOnlyList<Guid>> SeedFilesAsync(Guid companyId, IReadOnlyList<FileSeed> seeds)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var files = seeds
                .Select(seed => new FileRef(
                    seed.CompanyId ?? companyId,
                    seed.MachineId,
                    seed.Purpose ?? FilePurpose.Attachment,
                    seed.FileName,
                    seed.Extension,
                    AllowedFileTypes.FindByExtension(seed.Extension)!.MediaType,
                    seed.SizeBytes,
                    new byte[FileRef.Sha256Length],
                    description: null))
                .ToList();

            dbContext.FileRefs.AddRange(files);
            await dbContext.SaveChangesAsync();

            foreach (var (file, seed) in files.Zip(seeds))
            {
                await dbContext.FileRefs
                    .Where(candidate => candidate.Id == file.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(candidate => candidate.CreatedAt, seed.CreatedAt));
            }

            return files.Select(file => file.Id).ToList();
        }

        public async Task<GridResponse<FileGridItem>> SearchCompanyFilesAsync(
            Guid companyId,
            SearchFilesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchFilesRequest, GridResponse<FileGridItem>>>();

            return await handler.HandleAsync(request);
        }

        public async Task<GridResponse<FileGridItem>> SearchMachineFilesAsync(
            Guid companyId,
            SearchFilesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchFilesRequest, GridResponse<FileGridItem>>>();

            return await handler.HandleAsync(request);
        }

        public async Task UpdateCompanyFileAsync(
            Guid companyId,
            UpdateFileRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateFileRequest>>()
                .HandleAsync(request);
        }

        public async Task UpdateMachineFileAsync(
            Guid companyId,
            UpdateFileRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateFileRequest>>()
                .HandleAsync(request);
        }

        public async Task DeleteCompanyFileAsync(
            Guid companyId,
            DeleteFileRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteFileRequest>>()
                .HandleAsync(request);
        }

        public async Task DeleteMachineFileAsync(
            Guid companyId,
            DeleteFileRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            await scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteFileRequest>>()
                .HandleAsync(request);
        }

        public async Task<uint> LoadRowVersionAsync(Guid id)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .FileRefs
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .Where(file => file.Id == id)
                .Select(file => EF.Property<uint>(file, RowVersions.PropertyName))
                .SingleAsync();
        }

        public async Task<FileRef> LoadFileRowAsync(Guid id)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .FileRefs
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleAsync(file => file.Id == id);
        }

        public async Task DeleteFileRefAsync(Guid companyId, Guid id)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var file = await dbContext.FileRefs.SingleAsync(candidate => candidate.Id == id);

            dbContext.FileRefs.Remove(file);
            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<FileRef>> LoadFileRefsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .FileRefs
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<string>> LoadBlobNamesAsync()
        {
            var container = fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName);
            var names = new List<string>();

            await foreach (var blob in container.GetBlobsAsync())
            {
                names.Add(blob.Name);
            }

            return names;
        }

        public async Task<byte[]> ReadBlobAsync(Guid id)
        {
            var blob = fixture.BlobService.GetBlobContainerClient(DatabaseTestData.ContainerName)
                .GetBlobClient(id.ToString());
            var download = await blob.DownloadContentAsync();

            return download.Value.Content.ToArray();
        }
    }
}
