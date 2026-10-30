using System.Text.Json;
using HealthyFac.Auth.Application;
using HealthyFac.Auth.Application.Identities;
using HealthyFac.Auth.Application.Identities.Messages;
using HealthyFac.Auth.Application.Identities.ProvisionIdentity;
using HealthyFac.Auth.Application.Identities.RevokeIdentity;
using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Auth.IntegrationTests.Keycloak.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Revocation;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

public static class ProvisioningFixtureExtensions
{
    private const string UsersPath = "admin/realms/healthyfac/users";

    extension(ProvisioningFixture fixture)
    {
        public async Task ProvisionAsync(EmployeeAccepted message)
        {
            using var scope = fixture.CreateActorScope(message.CompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<EmployeeAccepted>>();

            await handler.HandleAsync(message, CancellationToken.None);
        }

        public async Task ProvisionAsync(EmployeeAccepted message, IIdentityAdmin identityAdmin)
        {
            using var scope = fixture.CreateActorScope(message.CompanyId);
            var handler = new ProvisionIdentityRequestHandler(
                identityAdmin,
                scope.ServiceProvider.GetRequiredService<IOutbox>(),
                scope.ServiceProvider.GetRequiredService<IAuthDbContext>(),
                scope.ServiceProvider.GetRequiredService<ILogger<ProvisionIdentityRequestHandler>>());

            await handler.HandleAsync(message, CancellationToken.None);
        }

        public async Task RevokeAsync(EmployeeDeactivated message)
        {
            using var scope = fixture.CreateActorScope(message.CompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<EmployeeDeactivated>>();

            await handler.HandleAsync(message, CancellationToken.None);
        }

        public async Task RevokeAsync(EmployeeDeactivated message, IIdentityAdmin identityAdmin)
        {
            using var scope = fixture.CreateActorScope(message.CompanyId);

            await fixture.RevokeAsync(
                message,
                identityAdmin,
                scope.ServiceProvider.GetRequiredService<ISessionRevoker>());
        }

        public async Task RevokeAsync(
            EmployeeDeactivated message,
            IIdentityAdmin identityAdmin,
            ISessionRevoker sessionRevoker
        )
        {
            using var scope = fixture.CreateActorScope(message.CompanyId);
            var handler = new RevokeIdentityRequestHandler(
                identityAdmin,
                sessionRevoker,
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                scope.ServiceProvider.GetRequiredService<ILogger<RevokeIdentityRequestHandler>>());

            await handler.HandleAsync(message, CancellationToken.None);
        }

        public async Task<RevocationEntry?> LoadRevocationAsync(Guid userId)
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(SystemActor.Create());
            var revokedAt = await scope.ServiceProvider.GetRequiredService<ITokenRevocationStore>()
                .GetRevokedAtAsync(userId, CancellationToken.None);
            if (revokedAt is null)
            {
                return null;
            }

            var timeToLive = await scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>()
                .GetDatabase()
                .KeyTimeToLiveAsync(RevocationKeys.For(userId));

            return new RevocationEntry { RevokedAt = revokedAt.Value, TimeToLive = timeToLive };
        }

        public async Task<ProvisionedUser> LoadProvisionedUserAsync(Guid employeeId, Guid companyId)
        {
            var userId = await fixture.Keycloak.Admin.FindUserIdByEmployeeIdAsync(employeeId, CancellationToken.None);
            if (userId is null)
            {
                return new ProvisionedUser { UserId = null, RoleNames = [], IsOrganizationMember = false };
            }

            var organizationId = await fixture.Keycloak.LoadOrganizationIdAsync(companyId);
            var memberIds = organizationId is null
                ? []
                : await fixture.Keycloak.LoadOrganizationMemberIdsAsync(organizationId.Value);
            var roleNames = await fixture.Keycloak.LoadRealmRoleNamesAsync(userId.Value);

            return new ProvisionedUser
            {
                UserId = userId,
                RoleNames = roleNames,
                IsOrganizationMember = memberIds.Contains(userId.Value),
            };
        }

        public Task<IReadOnlyList<IdentityProvisioned>> LoadProvisionedAsync(Guid employeeId)
            => fixture.LoadOutboxMessagesAsync<IdentityProvisioned>(message => message.EmployeeId == employeeId);

        public Task<IReadOnlyList<IdentityProvisioningFailed>> LoadProvisioningFailedAsync(Guid employeeId)
            => fixture.LoadOutboxMessagesAsync<IdentityProvisioningFailed>(message => message.EmployeeId == employeeId);

        public Task PublishAcceptedAsync(Guid messageId, EmployeeAccepted message)
            => fixture.PublishAsync(
                EmployeeAccepted.MessageType,
                JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
                messageId,
                ProvisioningTestData.ActorHeaders(message.CompanyId));

        public async Task WaitForInboxMessageProcessedAsync(Guid messageId)
        {
            using var timeout = new CancellationTokenSource(ProvisioningTestData.WaitTimeout);
            while (true)
            {
                using var scope = fixture.ScopeFactory.CreateActorScope(SystemActor.Create());
                var isProcessed = await scope.ServiceProvider.GetRequiredService<AuthDbContext>()
                    .InboxMessages
                    .AsNoTracking()
                    .AnyAsync(message => message.Id == messageId && message.ProcessedOn != null, timeout.Token);
                if (isProcessed)
                {
                    return;
                }

                await Task.Delay(ProvisioningTestData.PollDelay, timeout.Token);
            }
        }

        public async Task<int> CountInboxMessagesAsync(Guid messageId)
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(SystemActor.Create());

            return await scope.ServiceProvider.GetRequiredService<AuthDbContext>()
                .InboxMessages
                .AsNoTracking()
                .CountAsync(message => message.Id == messageId);
        }

        public async Task<int> CountMailsToAsync(string email)
        {
            var messages = await fixture.Keycloak.Mailpit.GetMessagesAsync();

            return messages.Count(message => message.To.Contains(email));
        }

        public async Task<int> CountUsersWithEmailAsync(string email)
        {
            var users = await fixture.Keycloak.Api.GetAsync<List<UserRepresentation>>(
                $"{UsersPath}?email={Uri.EscapeDataString(email)}&exact=true");

            return users.Count;
        }

        public Task CreateUserWithoutEmployeeIdAsync(string email)
            => fixture.Keycloak.Api.SendAsync(
                HttpMethod.Post,
                UsersPath,
                new { username = email, email, enabled = true, emailVerified = true });

        private async Task<IReadOnlyList<T>> LoadOutboxMessagesAsync<T>(Func<T, bool> isMatch)
            where T : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateActorScope(SystemActor.Create());
            var messageType = T.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<AuthDbContext>()
                .OutboxMessages
                .AsNoTracking()
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<T>(payload, JsonSerializerOptions.Web)!)
                .Where(isMatch)
                .ToList();
        }
    }

    public sealed record RevocationEntry
    {
        public required DateTimeOffset RevokedAt { get; init; }

        public required TimeSpan? TimeToLive { get; init; }
    }

    public sealed record ProvisionedUser
    {
        public required Guid? UserId { get; init; }

        public required IReadOnlyList<string> RoleNames { get; init; }

        public required bool IsOrganizationMember { get; init; }
    }

    private sealed record UserRepresentation
    {
        public required Guid Id { get; init; }
    }
}
