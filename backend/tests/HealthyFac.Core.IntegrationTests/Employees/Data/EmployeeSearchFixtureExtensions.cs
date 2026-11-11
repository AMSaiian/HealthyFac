using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.SearchEmployeeInvites;
using HealthyFac.Core.Application.Employees.SearchEmployees;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Arranges employees and invites and runs the employee read use cases as a user. Every method opens its own DI scope.
/// </summary>
public static class EmployeeSearchFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<EmployeeSeed> ArrangeEmployeeAsync(Guid companyId, EmployeeSeedOptions options)
        {
            var inviteId = await fixture.CreateInviteWithRoleAsync(
                companyId,
                options.Email,
                options.RoleCode,
                DateTime.UtcNow.AddHours(value: 1));
            var token = fixture.InviteTokenService.CreateToken(
                inviteId,
                (await fixture.LoadInviteAsync(inviteId)).TokenSalt);

            using var scope = fixture.ScopeFactory.CreateScope();
            var response = await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>>()
                .HandleAsync(
                    new AcceptEmployeeInviteRequest
                    {
                        Token = token,
                        FirstName = options.FirstName,
                        LastName = options.LastName,
                    });

            var identityUserId = Guid.CreateVersion7();
            await fixture.CompleteProvisioningAsync(response.EmployeeId, identityUserId);
            if (options.IsDeactivated)
            {
                await fixture.ChangeEmployeeAsync(response.EmployeeId, employee => employee.Deactivate());
            }

            return new EmployeeSeed { EmployeeId = response.EmployeeId, IdentityUserId = identityUserId };
        }

        public async Task<Guid> CreateInviteWithRoleAsync(
            Guid companyId,
            string email,
            RoleCode roleCode,
            DateTime expiresAt
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var roleId = await dbContext.Roles
                .Where(role => role.Code == roleCode)
                .Select(role => role.Id)
                .SingleAsync();
            var invite = new EmployeeInvite(
                companyId,
                email,
                roleId,
                fixture.InviteTokenService.CreateSalt(),
                expiresAt);

            dbContext.EmployeeInvites.Add(invite);
            await dbContext.SaveChangesAsync();

            return invite.Id;
        }

        public async Task<CurrentEmployeeResponse> GetCurrentEmployeeAsync(Guid companyId, Guid userId)
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = userId, CompanyId = companyId });

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetCurrentEmployeeRequest, CurrentEmployeeResponse>>()
                .HandleAsync(new GetCurrentEmployeeRequest());
        }

        public async Task<GridResponse<EmployeeRowResponse>> SearchEmployeesAsync(
            Guid companyId,
            SearchEmployeesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            return await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchEmployeesRequest, GridResponse<EmployeeRowResponse>>>()
                .HandleAsync(request);
        }

        public async Task<GridResponse<EmployeeInviteRowResponse>> SearchEmployeeInvitesAsync(
            Guid companyId,
            SearchEmployeeInvitesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);

            return await scope.ServiceProvider
                .GetRequiredService<
                    IRequestHandler<SearchEmployeeInvitesRequest, GridResponse<EmployeeInviteRowResponse>>>()
                .HandleAsync(request);
        }

        public async Task AcceptInviteByIdAsync(Guid inviteId)
        {
            var invite = await fixture.LoadInviteAsync(inviteId);

            await fixture.AcceptInviteAsync(fixture.InviteTokenService.CreateToken(inviteId, invite.TokenSalt));
        }
    }
}

public sealed record EmployeeSeedOptions
{
    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public RoleCode RoleCode { get; init; } = RoleCode.Worker;

    public bool IsDeactivated { get; init; }
}

public sealed record EmployeeSeed
{
    public required Guid EmployeeId { get; init; }

    public required Guid IdentityUserId { get; init; }
}
