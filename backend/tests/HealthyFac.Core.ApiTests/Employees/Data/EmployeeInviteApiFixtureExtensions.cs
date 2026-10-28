using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Testing.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Employees.Data;

public static class EmployeeInviteApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";

    extension(CoreApiFixture fixture)
    {
        public TestTokenClaims CreateSupervisorClaims() =>
            new()
            {
                Subject = Guid.NewGuid(),
                Organizations = [fixture.CompanyId.ToString()],
            };

        public async Task<HttpResponseMessage> PostCreateInviteAsync(
            TestTokenClaims? claims,
            IReadOnlyDictionary<string, object?>? extraFields = null
        )
        {
            using var client = fixture.CreateClient();

            if (claims is not null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    BearerScheme,
                    fixture.Tokens.CreateToken(claims));
            }

            var body = new JsonObject
            {
                ["email"] = $"{Guid.NewGuid():N}@healthyfac.test",
                ["roleCode"] = EmployeeInviteApiTestData.WorkerRoleCode,
                ["departmentIds"] = new JsonArray(fixture.IdEncryptor.Encrypt(fixture.DepartmentId)),
            };

            foreach (var (name, value) in extraFields ?? new Dictionary<string, object?>())
            {
                body[name] = JsonValue.Create(value);
            }

            return await client.PostAsJsonAsync(EmployeeInviteApiTestData.CreateInvitePath, body);
        }

        public async Task<HttpResponseMessage> PostVerifyInviteAnonymouslyAsync()
        {
            using var client = fixture.CreateClient();

            return await client.PostAsJsonAsync(
                EmployeeInviteApiTestData.VerifyInvitePath,
                new JsonObject { ["token"] = EmployeeInviteApiTestData.UnknownInviteToken });
        }

        public async Task<EmployeeInvite> LoadInviteAsync(HttpResponseMessage createResponse)
        {
            var created = await createResponse.Content.ReadFromJsonAsync<JsonObject>();
            var inviteId = fixture.IdEncryptor.Decrypt(created!["id"]!.GetValue<string>());

            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.EmployeeInvites.AsNoTracking().SingleAsync(invite => invite.Id == inviteId);
        }
    }
}
