using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

/// <summary>
/// A batch of status definitions of one machine. Each item creates the status with its code, or changes the status
/// that already has the code.
/// </summary>
public record ApplyStatusDefinitionChangesRequest : IRequest<ApplyStatusDefinitionChangesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public IReadOnlyList<StatusDefinitionChange> Items { get; init; } = [];
}

/// <summary>
/// The wanted state of the status with a code. A <see langword="null"/> severity or description means "no proposal":
/// a status that exists keeps its stored value, and a new status takes <see cref="StatusSeverity.Info"/> or no
/// description.
/// </summary>
public record StatusDefinitionChange
{
    public required string Code { get; init; }

    public StatusSeverity? Severity { get; init; }

    public string? Description { get; init; }
}

public record ApplyStatusDefinitionChangesResponse
{
    public required IReadOnlyList<AppliedStatusDefinition> Items { get; init; }
}

/// <summary>The result for one item, in the order of the request.</summary>
public record AppliedStatusDefinition
{
    /// <summary>The code as the machine stores it. A changed status keeps its stored code.</summary>
    public required string Code { get; init; }

    public required Guid Id { get; init; }

    public required bool IsCreated { get; init; }
}

public class ApplyStatusDefinitionChangesRequestValidator : AbstractValidator<ApplyStatusDefinitionChangesRequest>
{
    public ApplyStatusDefinitionChangesRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.MachineIdRequired);

        RuleFor(x => x.Items)
            .Must(items => items.Count is >= 1 and <= StatusDefinitionValidationConstants.ApplyMaxItems)
            .WithMessage(StatusDefinitionErrors.ApplyItemCountInvalid);

        RuleForEach(x => x.Items).SetValidator(new StatusDefinitionChangeValidator(richTextPolicy));

        RuleFor(x => x.Items)
            .Custom((items, context) =>
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < items.Count; index++)
                {
                    if (!seen.Add(items[index].Code ?? string.Empty))
                    {
                        context.AddFailure(
                            $"items[{index}].code",
                            StatusDefinitionErrors.CodeDuplicatedInRequest);
                    }
                }
            });
    }
}

public class StatusDefinitionChangeValidator : AbstractValidator<StatusDefinitionChange>
{
    public StatusDefinitionChangeValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Code).IsStatusCode();

        RuleFor(x => x.Description).IsStatusDefinitionDescription(richTextPolicy);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ApplyStatusDefinitionChangesPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<ApplyStatusDefinitionChangesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ApplyStatusDefinitionChangesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Creates or changes the status definitions of a machine in one save, so one transaction stores the whole batch.
/// </summary>
/// <remarks>
/// The pre-handler check has already checked the machine scope. The code column compares without case, so the lookup
/// finds a stored code that differs only in case, and the status keeps its stored code. An item with a
/// <see langword="null"/> severity or description is "no proposal": a status that exists keeps that stored value, as
/// the review save does, and a new status gets <see cref="StatusSeverity.Info"/> or no description. A status
/// definition adds no schema revision (the metric schema holds no status). The unique index on the machine and the
/// code is the only guard against a parallel create of the same code, and the handler maps its failure to the conflict.
/// </remarks>
/// <param name="dbContext">Loads and saves the status definitions.</param>
public class ApplyStatusDefinitionChangesRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<ApplyStatusDefinitionChangesRequest, ApplyStatusDefinitionChangesResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ApplyStatusDefinitionChangesResponse> HandleAsync(
        ApplyStatusDefinitionChangesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var codes = request.Items.Select(item => item.Code).ToList();

        var stored = await _dbContext.StatusDefinitions
            .Where(candidate => candidate.MachineRegistrationId == request.MachineId && codes.Contains(candidate.Code))
            .ToListAsync(cancellationToken);

        var storedByCode = stored.ToDictionary(status => status.Code, StringComparer.OrdinalIgnoreCase);
        var applied = new List<AppliedStatusDefinition>();

        foreach (var item in request.Items)
        {
            if (storedByCode.TryGetValue(item.Code, out var existing))
            {
                existing.Update(
                    existing.Code,
                    item.Severity ?? existing.Severity,
                    item.Description ?? existing.Description);
                applied.Add(new AppliedStatusDefinition { Code = existing.Code, Id = existing.Id, IsCreated = false });
                continue;
            }

            var created = new StatusDefinition(
                request.MachineId,
                item.Code,
                item.Severity ?? StatusSeverity.Info,
                item.Description);
            _dbContext.StatusDefinitions.Add(created);
            applied.Add(new AppliedStatusDefinition { Code = created.Code, Id = created.Id, IsCreated = true });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(StatusDefinitionErrors.CodeTaken, exception);
        }

        return new ApplyStatusDefinitionChangesResponse { Items = applied };
    }
}
