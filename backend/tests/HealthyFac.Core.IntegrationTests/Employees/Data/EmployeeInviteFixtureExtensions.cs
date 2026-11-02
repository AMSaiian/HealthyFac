using System.Text.Json;
using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Arranges and reads employee invite data for integration tests. Every database method opens its own DI scope, so
/// each step works with a fresh context, like a separate request, and reads what is stored, not tracked objects.
/// </summary>
public static class EmployeeInviteFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public async Task<ArrangedInvite> ArrangeInviteAsync(
            string email,
            DateTime expiresAt,
            params string[] departmentNames
        )
        {
            var companyId = await fixture.CreateCompanyAsync(InviteTestData.CompanyName);
            var departmentIds = await fixture.CreateDepartmentsAsync(companyId, departmentNames);
            var (inviteId, token) = await fixture.CreateInviteAsync(companyId, departmentIds, email, expiresAt);

            return new ArrangedInvite(companyId, departmentIds, inviteId, token);
        }

        public async Task<Guid> CreateCompanyAsync(string name)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var company = new Company { Name = name };

            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync();

            return company.Id;
        }

        public async Task<IReadOnlyList<Guid>> CreateDepartmentsAsync(Guid companyId, params string[] names)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var departments = names
                .Select(name => new Department
                {
                    CompanyId = companyId,
                    Name = name,
                    TimeZoneId = InviteTestData.DepartmentTimeZoneId,
                })
                .ToList();

            dbContext.Departments.AddRange(departments);
            await dbContext.SaveChangesAsync();

            return departments.Select(department => department.Id).ToList();
        }

        public async Task<(Guid InviteId, string Token)> CreateInviteAsync(
            Guid companyId,
            IReadOnlyList<Guid> departmentIds,
            string email,
            DateTime expiresAt
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var roleId = await dbContext.Roles
                .Where(role => role.Code == RoleCode.Worker)
                .Select(role => role.Id)
                .SingleAsync();
            var departments = await dbContext.Departments
                .Where(department => departmentIds.Contains(department.Id))
                .ToListAsync();
            var salt = fixture.InviteTokenService.CreateSalt();
            var invite = new EmployeeInvite(companyId, email, roleId, departments, salt, expiresAt);

            dbContext.EmployeeInvites.Add(invite);
            await dbContext.SaveChangesAsync();

            return (invite.Id, fixture.InviteTokenService.CreateToken(invite.Id, salt));
        }

        public async Task<Guid> AcceptInviteAsync(string token, string firstName = InviteTestData.EmployeeFirstName)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var response = await scope.ServiceProvider
                .GetRequiredService<IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>>()
                .HandleAsync(
                    new AcceptEmployeeInviteRequest
                    {
                        Token = token,
                        FirstName = firstName,
                        LastName = InviteTestData.EmployeeLastName,
                    });

            return response.EmployeeId;
        }

        public async Task<EmployeeInvite> LoadInviteAsync(Guid inviteId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .EmployeeInvites
                .AsNoTracking()
                .Include(invite => invite.Departments)
                .SingleAsync(invite => invite.Id == inviteId);
        }

        public async Task<Employee> LoadEmployeeAsync(Guid employeeId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Employees
                .AsNoTracking()
                .Include(employee => employee.Departments)
                .SingleAsync(employee => employee.Id == employeeId);
        }

        public async Task<int> CountInvitesAsync(Guid companyId, string email)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .EmployeeInvites
                .CountAsync(invite => invite.CompanyId == companyId && invite.Email == email);
        }

        public async Task<int> CountEmployeesAsync(string email)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Employees
                .CountAsync(employee => employee.Email == email);
        }

        public async Task<IReadOnlyList<TMessage>> ReadOutboxMessagesAsync<TMessage>()
            where TMessage : IIntegrationMessage
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var messageType = TMessage.MessageType;
            var payloads = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .OutboxMessages
                .Where(message => message.Type == messageType)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<TMessage>(payload, JsonSerializerOptions.Web)!)
                .ToList();
        }

        public async Task<string> CreateTokenFromStoredSaltAsync(Guid inviteId)
        {
            var invite = await fixture.LoadInviteAsync(inviteId);

            return fixture.InviteTokenService.CreateToken(inviteId, invite.TokenSalt);
        }

        public bool IsTokenValid(string token, byte[] tokenSalt)
            => fixture.InviteTokenService.IsValid(token, tokenSalt);
    }
}
