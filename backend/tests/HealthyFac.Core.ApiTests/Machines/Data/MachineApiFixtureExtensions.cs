using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Machines;
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
                DepartmentIds = [fixture.DepartmentId.ToString()],
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

        public string CreateSearchBodyWithDepartment(Guid departmentId) =>
            new JsonObject
            {
                ["offset"] = 0,
                ["limit"] = 25,
                ["filter"] = new JsonObject
                {
                    ["departmentIds"] = new JsonObject
                        { ["ids"] = new JsonArray(fixture.IdEncryptor.Encrypt(departmentId)) },
                },
            }.ToJsonString();

        public string CreateUpdateBody(string serialNumber) =>
            new JsonObject
            {
                ["name"] = MachineApiTestData.MachineName,
                ["serialNumber"] = serialNumber,
                ["departmentId"] = fixture.IdEncryptor.Encrypt(fixture.DepartmentId),
            }.ToJsonString();

        public async Task<string> ArrangeMachineAsync(string serialNumber)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var machine = new Machine(
                fixture.CompanyId,
                fixture.DepartmentId,
                MachineApiTestData.MachineName,
                serialNumber,
                null);

            dbContext.Machines.Add(machine);
            await dbContext.SaveChangesAsync();

            return fixture.IdEncryptor.Encrypt(machine.Id);
        }
    }
}
