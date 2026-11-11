using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using HealthyFac.Auth.Messaging.Identities;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Shifts.Data;

public static class ShiftApiFixtureExtensions
{
    private const string BearerScheme = "Bearer";
    private const string JsonMediaType = "application/json";

    extension(CoreApiFixture fixture)
    {
        public async Task<ShiftApiEmployee> ArrangeActiveEmployeeAsync(string email)
        {
            var identityUserId = Guid.CreateVersion7();
            using var scope = fixture.ScopeFactory.CreateActorScope(
                ServiceActor.DatabaseInitialization.For(ServiceName.Core));
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var tokens = scope.ServiceProvider.GetRequiredService<IInviteTokenService>();
            var roleId = await dbContext.Roles
                .Where(role => role.Code == RoleCode.Worker)
                .Select(role => role.Id)
                .SingleAsync();
            var salt = tokens.CreateSalt();
            var invite = new EmployeeInvite(fixture.CompanyId, email, roleId, salt, DateTime.UtcNow.AddHours(value: 1));
            dbContext.EmployeeInvites.Add(invite);
            await dbContext.SaveChangesAsync();

            var accepted = await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>>()
                .HandleAsync(
                    new AcceptEmployeeInviteRequest
                    {
                        Token = tokens.CreateToken(invite.Id, salt),
                        FirstName = ShiftApiTestData.FirstName,
                        LastName = ShiftApiTestData.LastName,
                    });
            await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<IdentityProvisioned>>()
                .HandleAsync(
                    new IdentityProvisioned { EmployeeId = accepted.EmployeeId, IdentityUserId = identityUserId },
                    CancellationToken.None);

            return new ShiftApiEmployee
            {
                EmployeeId = accepted.EmployeeId,
                EncryptedId = fixture.IdEncryptor.Encrypt(accepted.EmployeeId),
                IdentityUserId = identityUserId,
            };
        }

        public TestTokenClaims CreateShiftClaims(Guid subject, params string[] permissions) =>
            new()
            {
                Subject = subject,
                Organizations = [fixture.CompanyId.ToString()],
                Permissions = permissions,
            };

        public async Task<HttpResponseMessage> SendShiftRequestAsync(
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

        public string CreateVersionBody(string blockRange, int daysAhead)
        {
            var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysAhead);
            var parts = blockRange.Split(separator: '-');
            var days = new JsonArray();
            foreach (var weekday in new[]
                { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" })
            {
                days.Add(
                    new JsonObject
                    {
                        [propertyName: "weekday"] = weekday,
                        [propertyName: "blocks"] = new JsonArray(
                            new JsonObject { [propertyName: "start"] = parts[0], [propertyName: "end"] = parts[1] }),
                    });
            }

            return new JsonObject
            {
                [propertyName: "effectiveFrom"] = effectiveFrom.ToString(format: "yyyy-MM-dd"),
                [propertyName: "days"] = days,
            }.ToJsonString();
        }

        public async Task<string> ArrangeVersionAsync(ShiftApiEmployee employee, TestTokenClaims claims)
        {
            var response = await fixture.SendShiftRequestAsync(
                HttpMethod.Post,
                ShiftApiTestData.TemplatesPath(employee.EncryptedId),
                claims,
                fixture.CreateVersionBody(ShiftApiTestData.LatestEndBlock, ShiftApiTestData.EffectiveFromDaysAhead));

            return response.Headers.Location!.OriginalString.Split(separator: '/')[^1];
        }

        public string CreateOverrideBody() =>
            new JsonObject { [propertyName: "blocks"] = new JsonArray() }.ToJsonString();
    }
}

public sealed record ShiftApiEmployee
{
    public required Guid EmployeeId { get; init; }

    public required string EncryptedId { get; init; }

    public required Guid IdentityUserId { get; init; }
}
