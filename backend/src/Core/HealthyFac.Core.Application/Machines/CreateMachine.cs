using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Gateways;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record CreateMachineRequest : IRequest<CreateMachineResponse>
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid CompanyId { get; init; }

    public string? Description { get; init; }
}

public record CreateMachineResponse
{
    public required Guid Id { get; init; }
}

public class CreateMachineRequestValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Name).IsMachineName();

        RuleFor(x => x.SerialNumber).IsMachineSerialNumber();

        RuleFor(x => x.Description).IsMachineDescription(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);
    }
}

public sealed class CreateMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CreateMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates a machine in the company. The serial number must be unique among the machines of the
/// company that are not deleted, ignoring case.
/// </summary>
/// <remarks>
/// The handler checks the serial number first, so the common conflict costs no failed insert. The unique index
/// catches a parallel create of the same serial number, and the handler maps that failure to the same conflict.
/// </remarks>
/// <param name="dbContext">Reads the serial numbers and saves the machine.</param>
/// <param name="timeProvider">Provides the start of the first state of the machine.</param>
/// <param name="outbox">Queues the <see cref="MachineCreated"/> message in the save of the machine.</param>
public class CreateMachineRequestHandler(ICoreDbContext dbContext, TimeProvider timeProvider, IOutbox outbox)
    : IRequestHandler<CreateMachineRequest, CreateMachineResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOutbox _outbox = outbox;

    public async Task<CreateMachineResponse> HandleAsync(
        CreateMachineRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machine = new Machine(
            request.CompanyId,
            GatewayIds.Create(),
            request.Name,
            request.SerialNumber,
            request.Description,
            _timeProvider.GetUtcNow().UtcDateTime);

        var isSerialNumberTaken = await _dbContext.Machines.AnyAsync(
            candidate => candidate.CompanyId == machine.CompanyId
                && candidate.SerialNumber == machine.SerialNumber,
            cancellationToken);

        if (isSerialNumberTaken)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken);
        }

        _dbContext.Machines.Add(machine);
        _outbox.Add(
            new MachineCreated
            {
                MachineId = machine.Id,
                GatewayId = machine.GatewayId,
                CompanyId = machine.CompanyId,
                Name = machine.Name,
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken, exception);
        }

        return new CreateMachineResponse { Id = machine.Id };
    }
}
