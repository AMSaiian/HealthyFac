using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class RevokeEmployeeInviteTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RevokeAsync_WithOpenInvite_SoftDeletesItAndTheLinkStopsVerifying()
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            InviteTestData.RevokeOpenEmail,
            DateTime.UtcNow.AddHours(value: 1));
        var rowVersion = await LoadRowVersionAsync(arranged.InviteId);

        await RevokeAsync(arranged.CompanyId, arranged.InviteId, rowVersion);

        var isDeleted = await IsStoredAsDeletedAsync(arranged.InviteId);
        var verify = () => VerifyAsync(arranged.Token);

        isDeleted.Should().BeTrue();
        await verify.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task RevokeAsync_ThenANewInviteForTheSameEmail_IsPossible()
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            InviteTestData.RevokeAgainEmail,
            DateTime.UtcNow.AddHours(value: 1));
        var rowVersion = await LoadRowVersionAsync(arranged.InviteId);
        await RevokeAsync(arranged.CompanyId, arranged.InviteId, rowVersion);

        var again = await _fixture.CreateInviteAsync(
            arranged.CompanyId,
            InviteTestData.RevokeAgainEmail,
            DateTime.UtcNow.AddHours(value: 1));

        again.InviteId.Should().NotBe(arranged.InviteId);
    }

    [Fact]
    public async Task RevokeAsync_WithAcceptedInvite_ThrowsUnprocessable()
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            InviteTestData.RevokeAcceptedEmail,
            DateTime.UtcNow.AddHours(value: 1));
        await _fixture.AcceptInviteAsync(arranged.Token);
        var rowVersion = await LoadRowVersionAsync(arranged.InviteId);

        var act = () => RevokeAsync(arranged.CompanyId, arranged.InviteId, rowVersion);

        await act.Should()
            .ThrowAsync<UnprocessableException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteAlreadyAccepted);
    }

    [Fact]
    public async Task RevokeAsync_WithStaleRowVersion_ThrowsConflict()
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            InviteTestData.RevokeStaleEmail,
            DateTime.UtcNow.AddHours(value: 1));
        var staleRowVersion = await LoadRowVersionAsync(arranged.InviteId);
        await ResendAsync(arranged.CompanyId, arranged.InviteId);

        var act = () => RevokeAsync(arranged.CompanyId, arranged.InviteId, staleRowVersion);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteChanged);
    }

    [Fact]
    public async Task RevokeAsync_WithInviteOfOtherCompany_ThrowsNotFound()
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            InviteTestData.RevokeOtherCompanyEmail,
            DateTime.UtcNow.AddHours(value: 1));
        var otherCompanyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);
        var rowVersion = await LoadRowVersionAsync(arranged.InviteId);

        var act = () => RevokeAsync(otherCompanyId, arranged.InviteId, rowVersion);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteNotFound);
    }

    private async Task<uint> LoadRowVersionAsync(Guid inviteId)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
            .EmployeeInvites
            .AsNoTracking()
            .Where(invite => invite.Id == inviteId)
            .Select(invite => invite.RowVersion)
            .SingleAsync();
    }

    private async Task<bool> IsStoredAsDeletedAsync(Guid inviteId)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
            .EmployeeInvites
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AnyAsync(invite => invite.Id == inviteId && invite.DeletedAt != null);
    }

    private async Task RevokeAsync(Guid companyId, Guid inviteId, uint rowVersion)
    {
        using var scope = _fixture.CreateUserScope(companyId);
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<RevokeEmployeeInviteRequest>>();

        await handler.HandleAsync(
            new RevokeEmployeeInviteRequest { CompanyId = companyId, Id = inviteId, RowVersion = rowVersion });
    }

    private async Task ResendAsync(Guid companyId, Guid inviteId)
    {
        using var scope = _fixture.CreateUserScope(companyId);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>>();

        await handler.HandleAsync(new ResendEmployeeInviteRequest { CompanyId = companyId, InviteId = inviteId });
    }

    private async Task<VerifyEmployeeInviteResponse> VerifyAsync(string token)
    {
        using var scope = _fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<VerifyEmployeeInviteRequest, VerifyEmployeeInviteResponse>>()
            .HandleAsync(new VerifyEmployeeInviteRequest { Token = token });
    }
}
