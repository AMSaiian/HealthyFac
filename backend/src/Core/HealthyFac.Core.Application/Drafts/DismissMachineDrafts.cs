using FluentValidation;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record DismissMachineDraftsRequest : IRequest<DismissMachineDraftsResponse>
{
    public required Guid CompanyId { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? ChatId { get; init; }
}

public record DismissMachineDraftsResponse
{
    public required int DismissedCount { get; init; }
}

public class DismissMachineDraftsRequestValidator : AbstractValidator<DismissMachineDraftsRequest>
{
    public DismissMachineDraftsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(DraftErrors.CompanyIdRequired);

        RuleFor(request => request.MachineId)
            .NotEqual(Guid.Empty)
            .WithMessage(MachineErrors.MachineIdRequired)
            .When(request => request.MachineId is not null);

        RuleFor(request => request.AttemptId)
            .NotEqual(Guid.Empty)
            .WithMessage(DraftErrors.AttemptIdRequired)
            .When(request => request.AttemptId is not null);

        RuleFor(request => request.ChatId)
            .NotEqual(Guid.Empty)
            .WithMessage(DraftErrors.ChatIdRequired)
            .When(request => request.ChatId is not null);

        RuleFor(request => request)
            .Must(request => new[] { request.MachineId, request.AttemptId, request.ChatId }.Count(id => id is not null)
                == 1)
            .WithMessage(DraftErrors.SearchScopeRequired);
    }
}

/// <summary>
/// Checks the company of the actor and, for a machine scope, that the machine belongs to it. A machine of another
/// company is "not found".
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DismissMachineDraftsPreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DismissMachineDraftsRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DismissMachineDraftsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        if (request.MachineId is null)
        {
            return;
        }

        var isMachineInScope = await _dbContext.Machines.AnyAsync(
            machine => machine.Id == request.MachineId && machine.CompanyId == request.CompanyId,
            cancellationToken);

        if (!isMachineInScope)
        {
            throw new NotFoundException(MachineErrors.MachineNotFound);
        }
    }
}

/// <summary>
/// Dismisses every pending machine draft in the scope (the metric and status drafts live in Telemetry). The machine
/// scope holds the drafts that target the machine. The attempt scope holds every machine draft of the attempt. The chat
/// scope holds every machine draft of the chat. One save stores all changes, so a draft that changed in parallel is a
/// conflict and nothing is dismissed. Each create draft that it dismisses also queues
/// <see cref="MachineDraftDismissed"/> in the same save.
/// </summary>
/// <param name="dbContext">Loads the drafts and saves the change.</param>
/// <param name="outbox">Stores the messages with the change.</param>
public sealed class DismissMachineDraftsRequestHandler(ICoreDbContext dbContext, IOutbox outbox)
    : IRequestHandler<DismissMachineDraftsRequest, DismissMachineDraftsResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;

    /// <summary>
    /// Loads the pending drafts of the machine, dismisses them, and saves once.
    /// </summary>
    /// <param name="request">The company and the machine.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The number of dismissed drafts.</returns>
    /// <exception cref="ConflictException">Thrown when a draft changed while the use case ran.</exception>
    public async Task<DismissMachineDraftsResponse> HandleAsync(
        DismissMachineDraftsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machineDrafts = await _dbContext.MachineDrafts
            .Where(draft => draft.CompanyId == request.CompanyId
                && (request.MachineId == null || draft.TargetMachineId == request.MachineId)
                && (request.AttemptId == null || draft.Proposal.AttemptId == request.AttemptId)
                && (request.ChatId == null || draft.Proposal.ChatId == request.ChatId)
                && draft.State == DraftState.Pending)
            .ToListAsync(cancellationToken);

        machineDrafts.ForEach(draft => draft.Dismiss());

        foreach (var draft in machineDrafts.Where(draft => draft.IsCreate))
        {
            _outbox.Add(new MachineDraftDismissed { DraftId = draft.Id, CompanyId = draft.CompanyId });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DraftErrors.DraftChanged, exception);
        }

        return new DismissMachineDraftsResponse { DismissedCount = machineDrafts.Count };
    }
}
