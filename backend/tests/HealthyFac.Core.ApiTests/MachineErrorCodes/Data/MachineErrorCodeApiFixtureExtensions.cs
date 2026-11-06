using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.MachineErrorCodes;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.MachineErrorCodes.Data;

public static class MachineErrorCodeApiFixtureExtensions
{
    extension(CoreApiFixture fixture)
    {
        public string ErrorCodesPath(string encryptedMachineId) =>
            $"{MachineErrorCodeApiTestData.MachinesPath}/{encryptedMachineId}"
            + $"/{MachineErrorCodeApiTestData.ErrorCodesSegment}";

        public string CreateErrorCodeBody(string code) =>
            new JsonObject { [propertyName: "code"] = code }.ToJsonString();

        public async Task<EncryptedErrorCode> ArrangeErrorCodeAsync(string serialNumberSuffix, string code)
        {
            var encryptedMachineId = await fixture.ArrangeMachineAsync(
                MachineErrorCodeApiTestData.SerialNumberPrefix + serialNumberSuffix);
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var errorCode = new MachineErrorCode(
                fixture.IdEncryptor.Decrypt(encryptedMachineId), code, description: null);

            dbContext.MachineErrorCodes.Add(errorCode);
            await dbContext.SaveChangesAsync();

            return new EncryptedErrorCode(encryptedMachineId, fixture.IdEncryptor.Encrypt(errorCode.Id));
        }
    }
}

public sealed record EncryptedErrorCode(string MachineId, string Id);
