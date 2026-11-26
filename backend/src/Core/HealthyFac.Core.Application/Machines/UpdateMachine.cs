using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Machines.States;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record UpdateMachineRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public string? Description { get; init; }

    public required DateOnly ExpectedGoLiveOn { get; init; }

    public required MachineState State { get; init; }
}

public class UpdateMachineRequestValidator : AbstractValidator<UpdateMachineRequest>
{
    public UpdateMachineRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Name).IsMachineName();

        RuleFor(x => x.SerialNumber).IsMachineSerialNumber();

        RuleFor(x => x.Description).IsMachineDescription(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(MachineErrors.MachineIdRequired);

        RuleFor(x => x.State)
            .NotNull()
            .WithMessage(MachineErrors.StateRequired);
    }
}

public sealed class UpdateMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UpdateMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Changes the name, serial number, description, go-live date, and state of a machine, and queues
/// <see cref="MachineRenamed"/> in the same save when the name changes. Another state adds one state row that starts
/// now, and queues <see cref="MachineStateChanged"/>, in the same save. The current state adds no row. A new
/// go-live date queues <see cref="MachineGoLiveDateChanged"/> in the same save. A machine
/// outside the company is "not found". The serial number must stay unique among the machines of the company that
/// are not deleted, ignoring case.
/// </summary>
/// <remarks>
/// The handler checks the serial number against the other machines first. The unique index catches a parallel
/// change, and the handler maps that failure to the same conflict. A parallel state change with the same instant
/// breaks the history index before the row version check, and it gives <see cref="MachineErrors.MachineChanged"/>.
/// </remarks>
/// <param name="dbContext">Loads and saves the machine.</param>
/// <param name="outbox">Stores the rename message with the change.</param>
/// <param name="timeProvider">Provides the time of the rename and the start of a new state.</param>
/// <param name="companyZoneReader">Reads the company time zone for the expected go-live rule.</param>
public sealed class UpdateMachineRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    CompanyZoneReader companyZoneReader,
    IRealtimeNotifier notifier
)
    : RequestHandler<UpdateMachineRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly CompanyZoneReader _companyZoneReader = companyZoneReader;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        UpdateMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var machine = await _dbContext.Machines
                .Include(candidate => candidate.StateHistory)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        var zone = await _companyZoneReader.FindZoneAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        MachineGoLiveRules.EnsureNotBeforeCreation(request.ExpectedGoLiveOn, machine.CreatedAt, zone);

        var isRenamed = machine.Name != request.Name;
        var isGoLiveChanged = machine.ExpectedGoLiveOn != request.ExpectedGoLiveOn;
        machine.Update(request.Name, request.SerialNumber, request.Description, request.ExpectedGoLiveOn);

        var isStateChanged = MachineStateChanges.Apply(
            _dbContext, _outbox, machine, request.State, _timeProvider.GetUtcNow().UtcDateTime);

        var isSerialNumberTaken = await _dbContext.Machines.AnyAsync(
            candidate => candidate.CompanyId == machine.CompanyId
                && candidate.SerialNumber == machine.SerialNumber
                && candidate.Id != machine.Id,
            cancellationToken);

        if (isSerialNumberTaken)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken);
        }

        if (isRenamed)
        {
            _outbox.Add(
                new MachineRenamed
                {
                    MachineId = machine.Id,
                    CompanyId = machine.CompanyId,
                    Name = machine.Name,
                    RenamedAt = _timeProvider.GetUtcNow().UtcDateTime,
                });
        }

        if (isGoLiveChanged)
        {
            _outbox.Add(
                new MachineGoLiveDateChanged
                {
                    MachineId = machine.Id,
                    CompanyId = machine.CompanyId,
                    ExpectedGoLiveOn = machine.ExpectedGoLiveOn,
                    GoLiveStartsAt = MachineGoLiveRules.StartOfDayUtc(machine.ExpectedGoLiveOn, zone),
                    ChangedAt = _timeProvider.GetUtcNow().UtcDateTime,
                });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
            when (exception.ConstraintName == MachineStateHistoryIndexNames.MachineStartedAt)
        {
            throw new ConflictException(MachineErrors.MachineChanged, exception);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken, exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(MachineErrors.MachineChanged, exception);
        }

        if (isStateChanged)
        {
            _notifier.PushMachinesHint(request.CompanyId);
        }
    }
}
