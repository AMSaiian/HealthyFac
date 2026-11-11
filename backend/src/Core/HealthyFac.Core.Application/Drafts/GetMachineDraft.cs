using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record GetMachineDraftRequest : IRequest<MachineDraftResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public record MachineDraftResponse
{
    public required MachineDraftDetail Draft { get; init; }

    /// <summary>The current values of the target machine of an update draft, or <see langword="null"/>.</summary>
    public CurrentMachineValues? Current { get; init; }
}

/// <summary>
/// The current values of a machine, for the compare dialog of an update draft.
/// </summary>
public record CurrentMachineValues
{
    public required Guid MachineId { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public string? Description { get; init; }

    /// <summary>The row version of the machine. The apply of the draft sends it back.</summary>
    public required uint RowVersion { get; init; }
}

public class GetMachineDraftRequestValidator : AbstractValidator<GetMachineDraftRequest>
{
    public GetMachineDraftRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(DraftErrors.DraftIdRequired);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetMachineDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMachineDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetMachineDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one machine draft with its values. An update draft also returns the current values and the row version of
/// its target machine. A draft of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the draft and the machine.</param>
public sealed class GetMachineDraftRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<GetMachineDraftRequest, MachineDraftResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<MachineDraftResponse> HandleAsync(
        GetMachineDraftRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var draft = await _dbContext.MachineDrafts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(DraftErrors.DraftNotFound);

        CurrentMachineValues? current = null;

        if (draft.TargetMachineId is { } machineId)
        {
            current = await _dbContext.Machines
                .AsNoTracking()
                .Where(machine => machine.Id == machineId && machine.CompanyId == request.CompanyId)
                .Select(machine => new CurrentMachineValues
                {
                    MachineId = machine.Id,
                    Name = machine.Name,
                    SerialNumber = machine.SerialNumber,
                    Description = machine.Description,
                    RowVersion = machine.RowVersion,
                })
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new MachineDraftResponse
        {
            Draft = MachineDraftDetail.From(draft),
            Current = current,
        };
    }
}
