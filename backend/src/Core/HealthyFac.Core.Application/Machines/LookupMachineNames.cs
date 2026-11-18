using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record LookupMachineNamesRequest : IRequest<LookupMachineNamesResponse>
{
    public required Guid CompanyId { get; init; }

    public required IReadOnlyList<Guid> MachineIds { get; init; }

    public required IReadOnlyList<Guid> MachineDraftIds { get; init; }
}

public record LookupMachineNamesResponse
{
    public required IReadOnlyList<NamedMachine> Machines { get; init; }

    public required IReadOnlyList<NamedMachine> MachineDrafts { get; init; }
}

/// <summary>An ID and the name that goes with it.</summary>
public record NamedMachine
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}

public sealed class LookupMachineNamesRequestValidator : AbstractValidator<LookupMachineNamesRequest>
{
    public LookupMachineNamesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(request => request.MachineIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(MachineErrors.MachineIdsRequired)
            .Must(ids => ids.Count <= MachineValidationConstants.NameLookupMaxIds)
            .WithMessage(MachineErrors.NameLookupTooManyIds);

        RuleFor(request => request.MachineDraftIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(MachineErrors.MachineDraftIdsRequired)
            .Must(ids => ids.Count <= MachineValidationConstants.NameLookupMaxIds)
            .WithMessage(MachineErrors.NameLookupTooManyIds);
    }
}

public sealed class LookupMachineNamesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<LookupMachineNamesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(LookupMachineNamesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the names of machines and of proposed machines (create machine drafts) by ID, so the Gateway can show a
/// machine name next to a Telemetry draft. An ID that is unknown, belongs to another company, or names a deleted
/// machine is left out, so the answer does not show that it exists. A draft without a proposed name is left out too.
/// </summary>
/// <param name="dbContext">Reads the machines and the machine drafts.</param>
public sealed class LookupMachineNamesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<LookupMachineNamesRequest, LookupMachineNamesResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<LookupMachineNamesResponse> HandleAsync(
        LookupMachineNamesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machines = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machine.CompanyId == request.CompanyId && request.MachineIds.Contains(machine.Id))
            .Select(machine => new NamedMachine { Id = machine.Id, Name = machine.Name })
            .ToListAsync(cancellationToken);
        var machineDrafts = await _dbContext.MachineDrafts
            .AsNoTracking()
            .Where(draft => draft.CompanyId == request.CompanyId
                && request.MachineDraftIds.Contains(draft.Id)
                && draft.Name != null)
            .Select(draft => new NamedMachine { Id = draft.Id, Name = draft.Name! })
            .ToListAsync(cancellationToken);

        return new LookupMachineNamesResponse { Machines = machines, MachineDrafts = machineDrafts };
    }
}
