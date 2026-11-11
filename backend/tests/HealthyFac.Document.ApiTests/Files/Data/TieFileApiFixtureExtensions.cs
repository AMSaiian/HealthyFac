using System.Text.Json;
using HealthyFac.Document.ApiTests.Hosting;
using HealthyFac.Document.Application;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Messaging.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.ApiTests.Files.Data;

public static class TieFileApiFixtureExtensions
{
    extension(DocumentApiFixture fixture)
    {
        public async Task DeleteMachineRegistrationAsync(string encryptedMachineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var machineId = fixture.IdEncryptor.Decrypt(encryptedMachineId);
            var registration = await dbContext.MachineRegistrations.SingleAsync(candidate => candidate.Id == machineId);

            dbContext.MachineRegistrations.Remove(registration);
            await dbContext.SaveChangesAsync();
        }

        public async Task<uint> LoadRowVersionAsync(string encryptedFileId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var fileId = fixture.IdEncryptor.Decrypt(encryptedFileId);

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .FileRefs
                .Where(candidate => candidate.Id == fileId)
                .Select(candidate => candidate.RowVersion)
                .SingleAsync();
        }

        public async Task<string> ArrangeEmbeddedImageAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDocumentDbContext>();
            var image = new FileRef(
                fixture.CompanyId,
                machineId: null,
                FilePurpose.Embedded,
                fileName: "image",
                extension: "png",
                contentType: "image/png",
                sizeBytes: 10,
                new byte[FileRef.Sha256Length],
                description: null);

            dbContext.FileRefs.Add(image);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(image.Id);
        }

        public async Task<IReadOnlyList<FileMachineChanged>> ReadFileMachineChangedMessagesAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = FileMachineChanged.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<DocumentDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .OrderBy(message => message.OccurredOn)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<FileMachineChanged>(payload, JsonSerializerOptions.Web)!)
                .ToList();
        }
    }
}
