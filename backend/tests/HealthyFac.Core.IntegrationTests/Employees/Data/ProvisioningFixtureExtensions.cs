using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.DeactivateEmployee;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.CompleteProvisioning;
using HealthyFac.Core.Application.Employees.FailProvisioning;
using HealthyFac.Core.Application.Employees.RetryProvisioning;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Arranges employees in a provisioning state and runs the provisioning use cases. Every method opens its own DI
/// scope, so each step works with a fresh context, like a separate message or request.
/// </summary>
public static class ProvisioningFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<ProvisioningEmployee> ArrangeProvisioningEmployeeAsync(string email)
        {
            var invite = await fixture.ArrangeInviteAsync(
                email,
                DateTime.UtcNow.AddHours(1),
                InviteTestData.FirstDepartmentName);
            var employeeId = await fixture.AcceptInviteAsync(invite.Token);

            return new ProvisioningEmployee { CompanyId = invite.CompanyId, EmployeeId = employeeId };
        }

        public async Task ChangeEmployeeAsync(Guid employeeId, Action<Employee> change)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var employee = await dbContext.Employees.SingleAsync(employee => employee.Id == employeeId);

            change(employee);
            await dbContext.SaveChangesAsync();
        }

        public async Task CompleteProvisioningAsync(Guid employeeId, Guid identityUserId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<IdentityProvisioned>>();

            await handler.HandleAsync(
                new IdentityProvisioned { EmployeeId = employeeId, IdentityUserId = identityUserId },
                CancellationToken.None);
        }

        public async Task FailProvisioningAsync(Guid employeeId, string reason)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<IdentityProvisioningFailed>>();

            await handler.HandleAsync(
                new IdentityProvisioningFailed { EmployeeId = employeeId, Reason = reason },
                CancellationToken.None);
        }

        public async Task<RetryProvisioningResponse> RetryProvisioningAsync(Guid companyId, Guid employeeId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<RetryProvisioningRequest, RetryProvisioningResponse>>();

            return await handler.HandleAsync(
                new RetryProvisioningRequest { CompanyId = companyId, EmployeeId = employeeId },
                CancellationToken.None);
        }

        public async Task DeactivateEmployeeAsync(Guid companyId, Guid employeeId, Guid actorUserId)
        {
            using var scope = fixture.CreateActorScope(
                new ActorContext { Kind = ActorKind.User, UserId = actorUserId, CompanyId = companyId });
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeactivateEmployeeRequest>>();

            await handler.HandleAsync(
                new DeactivateEmployeeRequest { CompanyId = companyId, EmployeeId = employeeId },
                CancellationToken.None);
        }

        public async Task<IReadOnlyList<EmployeeDeactivated>> ReadDeactivatedMessagesAsync(Guid employeeId)
        {
            var messages = await fixture.ReadOutboxMessagesAsync<EmployeeDeactivated>();

            return messages.Where(message => message.EmployeeId == employeeId).ToList();
        }

        public async Task<IReadOnlyList<EmployeeAccepted>> ReadAcceptedMessagesAsync(Guid employeeId)
        {
            var messages = await fixture.ReadOutboxMessagesAsync<EmployeeAccepted>();

            return messages.Where(message => message.EmployeeId == employeeId).ToList();
        }
    }
}

public sealed record ProvisioningEmployee
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }
}
