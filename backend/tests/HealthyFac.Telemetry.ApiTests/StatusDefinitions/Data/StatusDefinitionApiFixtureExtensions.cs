using System.Text.Json.Nodes;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.StatusDefinitions.Data;

public static class StatusDefinitionApiFixtureExtensions
{
    extension(TelemetryApiFixture fixture)
    {
        public string StatusDefinitionsPath(string encryptedMachineId) =>
            $"{StatusDefinitionApiTestData.MachinesPath}/{encryptedMachineId}"
            + $"/{StatusDefinitionApiTestData.StatusDefinitionsSegment}";

        public string ApplyStatusDefinitionsBody(params string[] codes) =>
            new JsonObject
            {
                [propertyName: "items"] = new JsonArray(
                    codes.Select(code => (JsonNode)new JsonObject
                        {
                            [propertyName: "code"] = code,
                            [propertyName: "severity"] = StatusDefinitionApiTestData.Severity,
                        })
                        .ToArray()),
            }.ToJsonString();

        public string CreateStatusDefinitionBody(string code) =>
            new JsonObject
            {
                [propertyName: "code"] = code,
                [propertyName: "severity"] = StatusDefinitionApiTestData.Severity,
            }.ToJsonString();

        public string CreateStatusDefinitionBody(string code, string description) =>
            new JsonObject
            {
                [propertyName: "code"] = code,
                [propertyName: "severity"] = StatusDefinitionApiTestData.Severity,
                [propertyName: "description"] = description,
            }.ToJsonString();

        public async Task<string> ArrangeMachineAsync()
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var registration = new MachineRegistration(
                Guid.CreateVersion7(), GatewayIds.Create(), fixture.CompanyId);

            dbContext.MachineRegistrations.Add(registration);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(registration.Id);
        }

        public async Task<EncryptedStatusDefinition> ArrangeStatusDefinitionAsync(string code)
        {
            var encryptedMachineId = await fixture.ArrangeMachineAsync();
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var status = new StatusDefinition(
                fixture.IdEncryptor.Decrypt(encryptedMachineId), code, StatusSeverity.Info, description: null);

            dbContext.StatusDefinitions.Add(status);
            await dbContext.SaveChangesAsync();

            return new EncryptedStatusDefinition(encryptedMachineId, fixture.IdEncryptor.Encrypt(status.Id));
        }
    }
}

public sealed record EncryptedStatusDefinition(string MachineId, string Id);
