using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Infrastructure.CurrentUser;
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
                DepartmentIds = [fixture.DepartmentId.ToString()],
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

            var body = fixture.CreateInviteBody(extraFields);

            return await client.PostAsJsonAsync(EmployeeInviteApiTestData.CreateInvitePath, body);
        }

        /// <summary>
        /// Posts the invite as another service does: with a service token and the actor headers of the calling
        /// flow.
        /// </summary>
        /// <param name="claims">The claims of the service token.</param>
        /// <param name="actorId">The actor ID for the actor header.</param>
        /// <param name="companyId">The company for the company header.</param>
        /// <returns>The response of the endpoint.</returns>
        public async Task<HttpResponseMessage> PostCreateInviteAsServiceAsync(
            TestServiceTokenClaims claims,
            Guid actorId,
            Guid companyId
        )
        {
            using var client = fixture.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                fixture.Tokens.CreateServiceToken(claims));
            client.DefaultRequestHeaders.Add(ActorHeaderNames.ActorId, actorId.ToString());
            client.DefaultRequestHeaders.Add(ActorHeaderNames.CompanyId, companyId.ToString());

            return await client.PostAsJsonAsync(
                EmployeeInviteApiTestData.CreateInvitePath,
                fixture.CreateInviteBody(null));
        }

        private JsonObject CreateInviteBody(IReadOnlyDictionary<string, object?>? extraFields)
        {
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

            return body;
        }

        public async Task<HttpResponseMessage> PostVerifyInviteAnonymouslyAsync()
        {
            using var client = fixture.CreateClient();

            return await client.PostAsJsonAsync(
                EmployeeInviteApiTestData.VerifyInvitePath,
                new JsonObject { ["token"] = EmployeeInviteApiTestData.UnknownInviteToken });
        }

        public async Task<HttpResponseMessage> PostAcceptInviteAnonymouslyAsync(EmployeeInvite invite)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var token = scope.ServiceProvider.GetRequiredService<IInviteTokenService>()
                .CreateToken(invite.Id, invite.TokenSalt);
            using var client = fixture.CreateClient();

            return await client.PostAsJsonAsync(
                EmployeeInviteApiTestData.AcceptInvitePath,
                new JsonObject
                {
                    ["token"] = token,
                    ["firstName"] = EmployeeInviteApiTestData.AcceptedFirstName,
                    ["lastName"] = EmployeeInviteApiTestData.AcceptedLastName,
                });
        }

        public async Task<Employee> LoadEmployeeAsync(string email)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            return await dbContext.Employees.AsNoTracking().SingleAsync(employee => employee.Email == email);
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
