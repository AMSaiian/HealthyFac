using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.States;

public record GetMachineStateTimesRequest : IRequest<MachineStateTimesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}

/// <summary>
/// The time of a machine in each state over a range, in seconds. The state seconds and the seconds before the creation
/// add up to the length of the range.
/// </summary>
public record MachineStateTimesResponse
{
    public required IReadOnlyList<MachineStateTimeResponse> States { get; init; }

    /// <summary>The seconds of the range before the machine existed.</summary>
    public required double BeforeCreationSeconds { get; init; }
}

public record MachineStateTimeResponse
{
    /// <summary>The value of the state, for example <c>operational</c>.</summary>
    public required string State { get; init; }

    public required double Seconds { get; init; }
}

public class GetMachineStateTimesRequestValidator : AbstractValidator<GetMachineStateTimesRequest>
{
    public GetMachineStateTimesRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(MachineErrors.MachineIdRequired);

        RuleFor(request => request.To)
            .GreaterThan(request => request.From)
            .WithMessage(MachineErrors.RangeInvalid);
    }
}

public sealed class GetMachineStateTimesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMachineStateTimesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetMachineStateTimesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the time of a machine in each state over a range. A machine outside the company, and a deleted machine, are
/// "not found", so the answer does not show that the machine exists.
/// </summary>
/// <param name="dbContext">Reads the machine and its state history.</param>
public sealed class GetMachineStateTimesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<GetMachineStateTimesRequest, MachineStateTimesResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<MachineStateTimesResponse> HandleAsync(
        GetMachineStateTimesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isMachineInCompany = await _dbContext.Machines
            .AnyAsync(
                machine => machine.Id == request.MachineId && machine.CompanyId == request.CompanyId,
                cancellationToken);

        if (!isMachineInCompany)
        {
            throw new NotFoundException(MachineErrors.MachineNotFound);
        }

        var rows = await _dbContext.MachineStateHistories
            .AsNoTracking()
            .Where(row => row.MachineId == request.MachineId)
            .Select(row => new MachineStateSpan { State = row.State, StartedAt = row.StartedAt })
            .ToListAsync(cancellationToken);
        var times = MachineStateTimeCalculator.Calculate(rows, request.From, request.To);

        return new MachineStateTimesResponse
        {
            States = times.StateSeconds
                .Select(entry => new MachineStateTimeResponse { State = entry.Key, Seconds = entry.Value })
                .ToList(),
            BeforeCreationSeconds = times.BeforeCreationSeconds,
        };
    }
}
