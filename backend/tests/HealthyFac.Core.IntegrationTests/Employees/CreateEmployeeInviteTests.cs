using FluentValidation;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.CreateEmployeeInvite;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class CreateEmployeeInviteTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_StoresOpenInviteWithNormalizedEmail()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(companyId, InviteTestData.RawEmail, departmentIds);

        var invite = await fixture.LoadInviteAsync(inviteId);

        invite.Email.Should().Be(InviteTestData.NormalizedEmail);
        invite.AcceptedAt.Should().BeNull();
        invite.TokenSalt.Should().HaveCount(InviteTestData.ExpectedSaltBytes);
        invite.ExpiresAt.Should()
            .BeCloseTo(
                DateTime.UtcNow.AddHours(InviteTestData.ExpectedLifetimeHours),
                TimeSpan.FromMinutes(1));
        invite.Departments.Select(department => department.Id).Should().BeEquivalentTo(departmentIds);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsEmployeeInvitedOutboxRow()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(companyId, InviteTestData.OutboxEmail, departmentIds);

        var messages = await ReadInvitedMessagesAsync(inviteId);

        messages.Should().ContainSingle();
        messages[0].CompanyId.Should().Be(companyId);
        messages[0].Email.Should().Be(InviteTestData.OutboxEmail);
    }

    [Fact]
    public async Task CreateAsync_WithSameEmailInOtherCase_ThrowsConflict()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();
        await CreateAsync(companyId, InviteTestData.ConflictEmail, departmentIds);

        var act = () => CreateAsync(companyId, InviteTestData.ConflictEmailOtherCase, departmentIds);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.OpenInviteExists);
    }

    [Fact]
    public async Task CreateAsync_WithEmailOfExistingEmployee_ThrowsConflict()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();
        await ArrangeEmployeeAsync(companyId, departmentIds, InviteTestData.EmployedEmail);

        var act = () => CreateAsync(companyId, InviteTestData.EmployedEmail, departmentIds);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
    }

    [Fact]
    public async Task CreateAsync_WithDepartmentOfOtherCompany_ThrowsNotFound()
    {
        var (companyId, _) = await ArrangeCompanyAsync();
        var (_, foreignDepartmentIds) = await ArrangeCompanyAsync();

        var act = () => CreateAsync(companyId, InviteTestData.ForeignDepartmentEmail, foreignDepartmentIds);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.DepartmentsNotFound);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDepartmentIds_StoresEachDepartmentOnce()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(
            companyId,
            InviteTestData.DuplicateDepartmentEmail,
            [departmentIds[0], departmentIds[0]]);

        var invite = await fixture.LoadInviteAsync(inviteId);

        invite.Departments.Should().ContainSingle().Which.Id.Should().Be(departmentIds[0]);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEmail_ThrowsValidationException()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();

        var act = () => CreateAsync(companyId, InviteTestData.InvalidEmail, departmentIds);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_TwiceInParallelForSameEmail_CreatesOneInviteAndOneConflict()
    {
        var (companyId, departmentIds) = await ArrangeCompanyAsync();
        using var race = fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => CreateAsync(companyId, InviteTestData.CreateParallelEmail, departmentIds)),
            race.RunAsync(() => CreateAsync(companyId, InviteTestData.CreateParallelEmail, departmentIds)));

        var inviteCount = await fixture.CountInvitesAsync(
            companyId,
            InviteTestData.CreateParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.OpenInviteExists);

        inviteCount.Should().Be(1);
    }

    private async Task<(Guid CompanyId, IReadOnlyList<Guid> DepartmentIds)> ArrangeCompanyAsync()
    {
        var companyId = await fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var departmentIds = await fixture.CreateDepartmentsAsync(
            companyId,
            InviteTestData.FirstDepartmentName,
            InviteTestData.SecondDepartmentName);

        return (companyId, departmentIds);
    }

    private async Task ArrangeEmployeeAsync(Guid companyId, IReadOnlyList<Guid> departmentIds, string email)
    {
        var (_, token) = await fixture.CreateInviteAsync(
            companyId,
            departmentIds,
            email,
            DateTime.UtcNow.AddHours(1));

        await fixture.AcceptInviteAsync(token);
    }

    private async Task<IReadOnlyList<EmployeeInvited>> ReadInvitedMessagesAsync(Guid inviteId)
    {
        var messages = await fixture.ReadOutboxMessagesAsync<EmployeeInvited>();

        return messages.Where(message => message.InviteId == inviteId).ToList();
    }

    private async Task<Guid> CreateAsync(Guid companyId, string email, IReadOnlyList<Guid> departmentIds)
    {
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>>();

        var response = await handler.HandleAsync(
            new CreateEmployeeInviteRequest
            {
                CompanyId = companyId,
                Email = email,
                RoleCode = RoleCode.Worker,
                DepartmentIds = departmentIds,
            });

        return response.Id;
    }
}
