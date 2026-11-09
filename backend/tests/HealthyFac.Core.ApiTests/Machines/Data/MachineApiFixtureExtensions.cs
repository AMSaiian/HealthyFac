using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Domain.Gateways;
using HealthyFac.Testing.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Machines.Data;

public static class MachineApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";
    private const string JsonMediaType = "application/json";

    extension(CoreApiFixture fixture)
    {
        public TestTokenClaims CreateMachineClaims(params string[] permissions) =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendMachineRequestAsync(
            HttpMethod method,
            string path,
            TestTokenClaims claims,
            string? jsonBody = null
        )
        {
            using var client = fixture.CreateClient();
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateToken(claims));

            if (jsonBody is not null)
            {
                request.Content = new StringContent(jsonBody, Encoding.UTF8, JsonMediaType);
            }

            return await client.SendAsync(request);
        }

        public string CreateCreateBody(string description) =>
            new JsonObject
            {
                [propertyName: "name"] = MachineApiTestData.MachineName,
                [propertyName: "serialNumber"] = $"IMG-{Guid.NewGuid():N}",
                [propertyName: "description"] = description,
            }.ToJsonString();

        public string CreateUpdateBody(string serialNumber) =>
            new JsonObject
            {
                [propertyName: "name"] = MachineApiTestData.MachineName,
                [propertyName: "serialNumber"] = serialNumber,
            }.ToJsonString();

        public async Task<string> ArrangeMachineAsync(string serialNumber)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var machine = new Machine(
                fixture.CompanyId,
                GatewayIds.Create(),
                MachineApiTestData.MachineName,
                serialNumber,
                description: null,
                scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);

            dbContext.Machines.Add(machine);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(machine.Id);
        }
    }
}
