using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Document.Application;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.ApiTests.Files.Data;

public static class FileApiFixtureExtensions
{
    extension(DocumentApiFixture fixture)
    {
        public async Task<string> ArrangeMachineAsync(Guid? companyId = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var registration = new MachineRegistration(
                Guid.CreateVersion7(), companyId ?? fixture.CompanyId, name: "Test Machine");

            dbContext.MachineRegistrations.Add(registration);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(registration.Id);
        }

        public async Task<ArrangedFile> ArrangeFileAsync(string? encryptedMachineId = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var machineId = encryptedMachineId is null ? (Guid?)null : fixture.IdEncryptor.Decrypt(encryptedMachineId);
            var file = new FileRef(
                fixture.CompanyId,
                machineId,
                FilePurpose.Attachment,
                fileName: "manual",
                extension: "pdf",
                contentType: "application/pdf",
                sizeBytes: 10,
                new byte[FileRef.Sha256Length],
                description: null);

            dbContext.FileRefs.Add(file);
            await dbContext.SaveChangesAsync();

            var rowVersion = await dbContext.FileRefs
                .Where(candidate => candidate.Id == file.Id)
                .Select(candidate => candidate.RowVersion)
                .SingleAsync();

            return new ArrangedFile(fixture.IdEncryptor.Encrypt(file.Id), rowVersion);
        }

        public async Task<IReadOnlyList<FileRef>> LoadFileRefsAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .FileRefs
                .AsNoTracking()
                .ToListAsync();
        }
    }
}

public sealed record ArrangedFile(string EncryptedId, uint RowVersion);
