using FluentValidation;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record VerifyEmployeeInviteRequest : IRequest<VerifyEmployeeInviteResponse>
{
    [SensitiveData]
    public required string Token { get; init; }
}

public record VerifyEmployeeInviteResponse
{
    public required string Email { get; init; }
    public required string CompanyName { get; init; }
    public required RoleCode RoleCode { get; init; }
    public required IReadOnlyList<VerifiedInviteDepartment> Departments { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public record VerifiedInviteDepartment
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

public class VerifyEmployeeInviteRequestValidator : AbstractValidator<VerifyEmployeeInviteRequest>
{
    public VerifyEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.TokenRequired)
            .SingleLineText();
    }
}

public sealed class VerifyEmployeeInvitePreHandlerCheck : PassThroughPreHandlerCheck<VerifyEmployeeInviteRequest>;

public class VerifyEmployeeInviteRequestHandler(ICoreDbContext dbContext, EmployeeInviteResolver inviteResolver)
    : IRequestHandler<VerifyEmployeeInviteRequest, VerifyEmployeeInviteResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly EmployeeInviteResolver _inviteResolver = inviteResolver;

    public async Task<VerifyEmployeeInviteResponse> HandleAsync(
        VerifyEmployeeInviteRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var invite = await _inviteResolver.GetAcceptableAsync(request.Token, cancellationToken);

        var companyName = await _dbContext.Companies
            .Where(company => company.Id == invite.CompanyId)
            .Select(company => company.Name)
            .SingleAsync(cancellationToken);

        var roleCode = await _dbContext.Roles
                .Where(role => role.Id == invite.RoleId)
                .Select(role => role.Code)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        return new VerifyEmployeeInviteResponse
        {
            Email = invite.Email,
            CompanyName = companyName,
            RoleCode = roleCode,
            Departments = invite.Departments
                .Select(department => new VerifiedInviteDepartment { Id = department.Id, Name = department.Name })
                .ToList(),
            FirstName = invite.FirstName,
            LastName = invite.LastName,
            ExpiresAt = invite.ExpiresAt,
        };
    }
}
