using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Gateways;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Drafts;

public record ApplyMachineDraftRequest : IRequest<ApplyMachineDraftResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The date of a create draft from the drawer. Without it, a create draft gets today in the company time zone. An
    /// update draft keeps its stored date.
    /// </summary>
    public DateOnly? ExpectedGoLiveOn { get; init; }

    /// <summary>The row version of the machine that the client read. An update draft needs it.</summary>
    public uint? RowVersion { get; init; }
}

public record ApplyMachineDraftResponse
{
    public required Guid MachineId { get; init; }
}

public class ApplyMachineDraftRequestValidator : AbstractValidator<ApplyMachineDraftRequest>
{
    public ApplyMachineDraftRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(MachineErrors.CompanyIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(DraftErrors.DraftIdRequired);
        RuleFor(x => x.Name).IsMachineName();
        RuleFor(x => x.SerialNumber).IsMachineSerialNumber();
        RuleFor(x => x.Description).IsMachineDescription(richTextPolicy);
    }
}

/// <summary>
/// Checks the company of the actor.
/// </summary>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ApplyMachineDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ApplyMachineDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ApplyMachineDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Applies a pending machine draft with the values that the user confirmed. A create draft makes a new machine and
/// queues <see cref="MachineCreated"/> and <see cref="MachineDraftApplied"/> (Telemetry moves the metric and status
/// drafts of the draft to the machine). An update draft changes the target machine, checks its row version, and queues
/// <see cref="MachineRenamed"/> when the name changes. One save stores everything. A draft of another company is "not
/// found", and a draft that is not pending is unprocessable (422).
/// </summary>
/// <param name="dbContext">Loads the draft and the machines, and saves the change.</param>
/// <param name="outbox">Stores the messages with the change.</param>
/// <param name="timeProvider">Provides the start of the first machine state and the time of a rename.</param>
/// <param name="companyZoneReader">Reads the company time zone for the expected go-live rule.</param>
public sealed class ApplyMachineDraftRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    CompanyZoneReader companyZoneReader
)
    : IRequestHandler<ApplyMachineDraftRequest, ApplyMachineDraftResponse>
{
    private const string RowVersionKey = "rowVersion";

    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly CompanyZoneReader _companyZoneReader = companyZoneReader;

    public async Task<ApplyMachineDraftResponse> HandleAsync(
        ApplyMachineDraftRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var draft = await _dbContext.MachineDrafts.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(DraftErrors.DraftNotFound);

        if (draft.State != DraftState.Pending)
        {
            throw new UnprocessableException(DraftErrors.DraftNotPending);
        }

        var machineId = draft.IsCreate
            ? await CreateMachineAsync(request, draft, cancellationToken)
            : await UpdateMachineAsync(request, draft, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken, exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DraftErrors.DraftChanged, exception);
        }

        return new ApplyMachineDraftResponse { MachineId = machineId };
    }

    private async Task<Guid> CreateMachineAsync(
        ApplyMachineDraftRequest request,
        MachineDraft draft,
        CancellationToken cancellationToken
    )
    {
        var zone = await _companyZoneReader.FindZoneAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expectedGoLiveOn = request.ExpectedGoLiveOn ?? MachineGoLiveRules.DayOf(nowUtc, zone);
        MachineGoLiveRules.EnsureNotBeforeToday(expectedGoLiveOn, nowUtc, zone);

        await EnsureSerialNumberFreeAsync(request, excludedMachineId: null, cancellationToken);

        var machine = new Machine(
            request.CompanyId,
            GatewayIds.Create(),
            request.Name,
            request.SerialNumber,
            request.Description,
            expectedGoLiveOn,
            nowUtc);

        _dbContext.Machines.Add(machine);

        _outbox.Add(
            new MachineCreated
            {
                MachineId = machine.Id,
                GatewayId = machine.GatewayId,
                CompanyId = machine.CompanyId,
                Name = machine.Name,
                ExpectedGoLiveOn = machine.ExpectedGoLiveOn,
                GoLiveStartsAt = MachineGoLiveRules.StartOfDayUtc(machine.ExpectedGoLiveOn, zone),
            });

        draft.MarkApplied(machine.Id);

        _outbox.Add(
            new MachineDraftApplied
            {
                DraftId = draft.Id,
                FileId = draft.FileId,
                MachineId = machine.Id,
                CompanyId = machine.CompanyId,
                QueuedAt = draft.QueuedAt,
            });

        return machine.Id;
    }

    private async Task<Guid> UpdateMachineAsync(
        ApplyMachineDraftRequest request,
        MachineDraft draft,
        CancellationToken cancellationToken
    )
    {
        MachineGoLiveRules.EnsureNoDateOnUpdateDraft(request.ExpectedGoLiveOn);

        if (request.RowVersion is not { } rowVersion)
        {
            throw new AppValidationException([new ValidationFailure(RowVersionKey, DraftErrors.RowVersionRequired)]);
        }

        var machine = await _dbContext.Machines.SingleOrDefaultAsync(
                candidate => candidate.Id == draft.TargetMachineId && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        await EnsureSerialNumberFreeAsync(request, machine.Id, cancellationToken);

        _dbContext.Entry(machine).Property(stored => stored.RowVersion).OriginalValue = rowVersion;
        var isRenamed = machine.Name != request.Name;
        machine.Update(request.Name, request.SerialNumber, request.Description, machine.ExpectedGoLiveOn);

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

        draft.MarkApplied(machine.Id);

        return machine.Id;
    }

    private async Task EnsureSerialNumberFreeAsync(
        ApplyMachineDraftRequest request,
        Guid? excludedMachineId,
        CancellationToken cancellationToken
    )
    {
        var isSerialNumberTaken = await _dbContext.Machines.AnyAsync(
            candidate => candidate.CompanyId == request.CompanyId
                && candidate.SerialNumber == request.SerialNumber
                && candidate.Id != excludedMachineId,
            cancellationToken);

        if (isSerialNumberTaken)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken);
        }
    }
}
