using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.ResendEmployeeInvite;
using HealthyFac.Core.Application.Employees.VerifyEmployeeInvite;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class VerifyEmployeeInviteTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task VerifyAsync_WithValidToken_ReturnsPrefillData()
    {
        var expiresAt = NextHour();
        var arranged = await fixture.ArrangeInviteAsync(
            InviteTestData.VerifyValidEmail,
            expiresAt,
            InviteTestData.FirstDepartmentName,
            InviteTestData.SecondDepartmentName);

        var response = await VerifyAsync(arranged.Token);

        response.Email.Should().Be(InviteTestData.VerifyValidEmail);
        response.CompanyName.Should().Be(InviteTestData.CompanyName);
        response.RoleCode.Should().Be(RoleCode.Worker);
        response.ExpiresAt.Should().Be(expiresAt);
        response.Departments.Select(department => department.Name)
            .Should()
            .BeEquivalentTo([InviteTestData.FirstDepartmentName, InviteTestData.SecondDepartmentName]);
        response.Departments.Select(department => department.Id).Should().BeEquivalentTo(arranged.DepartmentIds);
    }

    [Fact]
    public async Task VerifyAsync_WithExpiredInvite_ThrowsGenericNotFound()
    {
        var arranged = await fixture.ArrangeInviteAsync(
            InviteTestData.VerifyExpiredEmail,
            DateTime.UtcNow.AddHours(-1),
            InviteTestData.FirstDepartmentName);

        var act = () => VerifyAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task VerifyAsync_WithAcceptedInvite_ThrowsGenericNotFound()
    {
        var arranged = await fixture.ArrangeInviteAsync(
            InviteTestData.VerifyAcceptedEmail,
            NextHour(),
            InviteTestData.FirstDepartmentName);
        await fixture.AcceptInviteAsync(arranged.Token);

        var act = () => VerifyAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task VerifyAsync_WithTamperedToken_ThrowsGenericNotFound()
    {
        var arranged = await fixture.ArrangeInviteAsync(
            InviteTestData.VerifyTamperedEmail,
            NextHour(),
            InviteTestData.FirstDepartmentName);
        var tamperedToken = arranged.Token[..^1] + (arranged.Token[^1] == 'A' ? 'B' : 'A');

        var act = () => VerifyAsync(tamperedToken);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task VerifyAsync_WithTokenOfUnknownInvite_ThrowsGenericNotFound()
    {
        var token = CreateTokenForUnknownInvite();

        var act = () => VerifyAsync(token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task VerifyAsync_AfterResend_RejectsOldTokenAndAcceptsNewToken()
    {
        var arranged = await fixture.ArrangeInviteAsync(
            InviteTestData.VerifyResendEmail,
            NextHour(),
            InviteTestData.FirstDepartmentName);

        await ResendAsync(arranged.CompanyId, arranged.InviteId);

        var newToken = await fixture.CreateTokenFromStoredSaltAsync(arranged.InviteId);
        var newTokenResponse = await VerifyAsync(newToken);
        var verifyOldToken = () => VerifyAsync(arranged.Token);

        await verifyOldToken.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
        newTokenResponse.Email.Should().Be(InviteTestData.VerifyResendEmail);
    }

    private static DateTime NextHour() =>
        DateTime.UtcNow.AddHours(1).Date.AddHours(DateTime.UtcNow.AddHours(1).Hour);

    private string CreateTokenForUnknownInvite()
        => fixture.InviteTokenService.CreateToken(Guid.NewGuid(), fixture.InviteTokenService.CreateSalt());

    private async Task ResendAsync(Guid companyId, Guid inviteId)
    {
        using var scope = fixture.ScopeFactory.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>>()
            .HandleAsync(new ResendEmployeeInviteRequest { CompanyId = companyId, InviteId = inviteId });
    }

    private async Task<VerifyEmployeeInviteResponse> VerifyAsync(string token)
    {
        using var scope = fixture.ScopeFactory.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<IRequestHandler<VerifyEmployeeInviteRequest, VerifyEmployeeInviteResponse>>()
            .HandleAsync(new VerifyEmployeeInviteRequest { Token = token });
    }
}
