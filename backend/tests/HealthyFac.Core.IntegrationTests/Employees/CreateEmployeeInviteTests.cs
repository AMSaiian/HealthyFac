using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class CreateEmployeeInviteTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateAsync_WithValidRequest_StoresOpenInviteWithNormalizedEmail()
    {
        var companyId = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(companyId, InviteTestData.RawEmail);

        var invite = await _fixture.LoadInviteAsync(inviteId);

        invite.Email.Should().Be(InviteTestData.NormalizedEmail);
        invite.AcceptedAt.Should().BeNull();
        invite.TokenSalt.Should().HaveCount(InviteTestData.ExpectedSaltBytes);
        invite.ExpiresAt.Should()
            .BeCloseTo(
                DateTime.UtcNow.AddHours(InviteTestData.ExpectedLifetimeHours),
                TimeSpan.FromMinutes(minutes: 1));
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsEmployeeInvitedOutboxRow()
    {
        var companyId = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(companyId, InviteTestData.OutboxEmail);

        var messages = await ReadInvitedMessagesAsync(inviteId);

        messages.Should().ContainSingle();
        messages[index: 0].CompanyId.Should().Be(companyId);
        messages[index: 0].Email.Should().Be(InviteTestData.OutboxEmail);
    }

    [Fact]
    public async Task CreateAsync_WithNames_StoresThemOnTheInvite()
    {
        var companyId = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(
            companyId,
            InviteTestData.NamedEmail,
            InviteTestData.InvitedFirstName,
            InviteTestData.InvitedLastName);

        var invite = await _fixture.LoadInviteAsync(inviteId);

        invite.FirstName.Should().Be(InviteTestData.InvitedFirstName);
        invite.LastName.Should().Be(InviteTestData.InvitedLastName);
    }

    [Fact]
    public async Task CreateAsync_WithoutNames_StoresNullNames()
    {
        var companyId = await ArrangeCompanyAsync();

        var inviteId = await CreateAsync(companyId, InviteTestData.UnnamedEmail);

        var invite = await _fixture.LoadInviteAsync(inviteId);

        invite.FirstName.Should().BeNull();
        invite.LastName.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithSameEmailInOtherCase_ThrowsConflict()
    {
        var companyId = await ArrangeCompanyAsync();
        await CreateAsync(companyId, InviteTestData.ConflictEmail);

        var act = () => CreateAsync(companyId, InviteTestData.ConflictEmailOtherCase);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.OpenInviteExists);
    }

    [Fact]
    public async Task CreateAsync_WithEmailOfExistingEmployee_ThrowsConflict()
    {
        var companyId = await ArrangeCompanyAsync();
        await ArrangeEmployeeAsync(companyId, InviteTestData.EmployedEmail);

        var act = () => CreateAsync(companyId, InviteTestData.EmployedEmail);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
    }

    [Fact]
    public async Task CreateAsync_TwiceInParallelForSameEmail_CreatesOneInviteAndOneConflict()
    {
        var companyId = await ArrangeCompanyAsync();
        using var race = _fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => CreateAsync(companyId, InviteTestData.CreateParallelEmail)),
            race.RunAsync(() => CreateAsync(companyId, InviteTestData.CreateParallelEmail)));

        var inviteCount = await _fixture.CountInvitesAsync(
            companyId,
            InviteTestData.CreateParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.OpenInviteExists);

        inviteCount.Should().Be(expected: 1);
    }

    private Task<Guid> ArrangeCompanyAsync() => _fixture.CreateCompanyAsync(InviteTestData.CompanyName);

    private async Task ArrangeEmployeeAsync(Guid companyId, string email)
    {
        var (_, token) = await _fixture.CreateInviteAsync(
            companyId,
            email,
            DateTime.UtcNow.AddHours(value: 1));

        await _fixture.AcceptInviteAsync(token);
    }

    private async Task<IReadOnlyList<EmployeeInvited>> ReadInvitedMessagesAsync(Guid inviteId)
    {
        var messages = await _fixture.ReadOutboxMessagesAsync<EmployeeInvited>();

        return messages.Where(message => message.InviteId == inviteId).ToList();
    }

    private async Task<Guid> CreateAsync(
        Guid companyId,
        string email,
        string? firstName = null,
        string? lastName = null
    )
    {
        using var scope = _fixture.CreateUserScope(companyId);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>>();

        var response = await handler.HandleAsync(
            new CreateEmployeeInviteRequest
            {
                CompanyId = companyId,
                Email = email,
                RoleCode = RoleCode.Worker,
                FirstName = firstName,
                LastName = lastName,
            });

        return response.Id;
    }
}
