using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricUnits;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

/// <summary>
/// The staged changes of the metric definitions of one machine. The person stages changes in a list and sends them
/// together; one save stores them and a new schema revision.
/// </summary>
public record ApplyMetricDefinitionChangesRequest : IRequest<ApplyMetricDefinitionChangesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    /// <summary>The revision that the person saw. A different stored revision is a conflict.</summary>
    public required int ExpectedRevision { get; init; }

    public IReadOnlyList<CreatedMetricDefinition> Created { get; init; } = [];

    public IReadOnlyList<UpdatedMetricDefinition> Updated { get; init; } = [];

    public IReadOnlyList<Guid> Deleted { get; init; } = [];
}

public record ApplyMetricDefinitionChangesResponse
{
    public required int Revision { get; init; }
}

/// <summary>A new metric definition. The key, the value kind, and the unit are fixed from now on.</summary>
public record CreatedMetricDefinition : IMetricLimitsInput
{
    /// <summary>The metric draft that this definition applies, or <see langword="null"/>.</summary>
    public Guid? DraftId { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required MetricValueKind? ValueKind { get; init; }

    public MetricUnitReference? Unit { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    /// <summary>How the readings behave. A missing value means <see cref="MetricBehavior.Level"/>.</summary>
    public MetricBehavior? Behavior { get; init; }
}

/// <summary>A change of the editable fields of a metric definition. It has no key, value kind, or unit.</summary>
public record UpdatedMetricDefinition : IMetricLimitsInput
{
    public required Guid Id { get; init; }

    /// <summary>The metric draft that this change applies, or <see langword="null"/>.</summary>
    public Guid? DraftId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    /// <summary>The limits replace the stored ones: a missing limit removes it.</summary>
    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    /// <summary>How the readings behave. A missing value means <see cref="MetricBehavior.Level"/>.</summary>
    public MetricBehavior? Behavior { get; init; }
}

/// <summary>A unit of the catalog by the pair of its quantity kind name and its unit name.</summary>
public record MetricUnitReference
{
    public required string QuantityKind { get; init; }

    public required string Name { get; init; }
}

public class ApplyMetricDefinitionChangesRequestValidator : AbstractValidator<ApplyMetricDefinitionChangesRequest>
{
    public ApplyMetricDefinitionChangesRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.MachineIdRequired);

        RuleFor(x => x.ExpectedRevision)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(MetricDefinitionErrors.ExpectedRevisionInvalid);

        RuleForEach(x => x.Created).SetValidator(new CreatedMetricDefinitionValidator(richTextPolicy));
        RuleForEach(x => x.Updated).SetValidator(new UpdatedMetricDefinitionValidator(richTextPolicy));

        RuleForEach(x => x.Deleted)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.IdRequired);

        RuleFor(x => x.Created)
            .Must(created => created.Select(metric => metric.Key).Distinct(StringComparer.Ordinal).Count()
                == created.Count)
            .WithMessage(MetricDefinitionErrors.KeyDuplicatedInRequest);

        RuleFor(x => x.Updated)
            .Must((request, updated) => updated.Select(metric => metric.Id).Concat(request.Deleted).Distinct().Count()
                == updated.Count + request.Deleted.Count)
            .WithMessage(MetricDefinitionErrors.IdDuplicatedInRequest);
    }
}

public class CreatedMetricDefinitionValidator : AbstractValidator<CreatedMetricDefinition>
{
    public CreatedMetricDefinitionValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Key).IsMetricKey();

        RuleFor(x => x.Name).IsMetricName();

        RuleFor(x => x.Description).IsMetricDescription(richTextPolicy);

        RuleFor(x => x.ValueKind)
            .NotNull()
            .WithMessage(MetricDefinitionErrors.ValueKindRequired);

        When(
            x => x.ValueKind == MetricValueKind.Numeric, () =>
            {
                RuleFor(x => x.Unit)
                    .NotNull()
                    .WithMessage(MetricDefinitionErrors.UnitRequired);
            });

        RuleFor(x => x.Unit!)
            .SetValidator(new MetricUnitReferenceValidator())
            .When(x => x.Unit is not null);

        RuleFor(x => x.Unit)
            .Must(unit => unit!.QuantityKind != DiscreteUnits.KindName)
            .WithMessage(MetricDefinitionErrors.UnitDiscreteNotAllowed)
            .When(x => x.ValueKind == MetricValueKind.Numeric && x.Unit is not null);

        When(
            x => x.ValueKind is not null && x.ValueKind != MetricValueKind.Numeric, () =>
            {
                RuleFor(x => x.Unit)
                    .Null()
                    .WithMessage(MetricDefinitionErrors.UnitNotAllowed);

                RuleFor(x => x.Min)
                    .Null()
                    .WithMessage(MetricDefinitionErrors.RangeNotAllowed);

                RuleFor(x => x.Max)
                    .Null()
                    .WithMessage(MetricDefinitionErrors.RangeNotAllowed);
            });

        RuleFor(x => x.Max)
            .GreaterThan(x => x.Min!.Value)
            .WithMessage(MetricDefinitionErrors.RangeNotAscending)
            .When(x => x.Min is not null && x.Max is not null);

        When(
            x => x.ValueKind == MetricValueKind.StateSet, () =>
            {
                RuleFor(x => x.States)
                    .NotNull()
                    .WithMessage(MetricDefinitionErrors.StatesRequired);
            });

        When(
            x => x.ValueKind is not null && x.ValueKind != MetricValueKind.StateSet, () =>
            {
                RuleFor(x => x.States)
                    .Null()
                    .WithMessage(MetricDefinitionErrors.StatesNotAllowed);
            });

        RuleFor(x => x.States).HasValidStates();

        RuleForEach(x => x.States!).SingleLineText().When(x => x.States is not null);

        this.AddMetricLimitRules();

        When(
            x => x.ValueKind is not null && x.ValueKind != MetricValueKind.Numeric, () =>
            {
                RuleFor(x => x.WarningLow).Null().WithMessage(MetricDefinitionErrors.LimitsNotAllowed);
                RuleFor(x => x.WarningHigh).Null().WithMessage(MetricDefinitionErrors.LimitsNotAllowed);
                RuleFor(x => x.AlarmLow).Null().WithMessage(MetricDefinitionErrors.LimitsNotAllowed);
                RuleFor(x => x.AlarmHigh).Null().WithMessage(MetricDefinitionErrors.LimitsNotAllowed);
            });
    }
}

public class MetricUnitReferenceValidator : AbstractValidator<MetricUnitReference>
{
    public MetricUnitReferenceValidator()
    {
        RuleFor(x => x.QuantityKind)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.UnitQuantityRequired)
            .SingleLineText();

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.UnitNameRequired)
            .SingleLineText();
    }
}

public class UpdatedMetricDefinitionValidator : AbstractValidator<UpdatedMetricDefinition>
{
    public UpdatedMetricDefinitionValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(MetricDefinitionErrors.IdRequired);

        RuleFor(x => x.Name).IsMetricName();

        RuleFor(x => x.Description).IsMetricDescription(richTextPolicy);

        RuleFor(x => x.Max)
            .GreaterThan(x => x.Min!.Value)
            .WithMessage(MetricDefinitionErrors.RangeNotAscending)
            .When(x => x.Min is not null && x.Max is not null);

        RuleFor(x => x.States).HasValidStates();

        RuleForEach(x => x.States!).SingleLineText().When(x => x.States is not null);

        this.AddMetricLimitRules();
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, a
/// deleted machine, and an unknown machine are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine registration.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ApplyMetricDefinitionChangesPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<ApplyMetricDefinitionChangesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ApplyMetricDefinitionChangesRequest request, CancellationToken cancellationToken)
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
/// Applies the staged changes of the metric definitions of a machine in one save: the created, updated, and deleted
/// definitions, the raised revision number of the registration, and the new schema revision. A request that changes no
/// definition (only equal updates that mark drafts) raises no revision and stores no schema revision; it still marks
/// the drafts and returns the current revision.
/// </summary>
/// <remarks>
/// A stale <c>ExpectedRevision</c> is a conflict, and the row version of the registration catches a parallel apply
/// that read the same revision: the second save finds a changed row and the handler maps that failure to the same
/// conflict. The unique index on the machine and the key catches a parallel create of the same key. The handler never
/// retries, because the person must reload and decide again.
/// </remarks>
/// <param name="dbContext">Loads and saves the registration, the definitions, and the schema revision.</param>
/// <param name="timeProvider">Provides the time of the schema revision.</param>
/// <param name="metricSchemaGenerator">Builds the schema of the new revision.</param>
public sealed class ApplyMetricDefinitionChangesRequestHandler(
    ITelemetryDbContext dbContext,
    TimeProvider timeProvider,
    IMetricSchemaGenerator metricSchemaGenerator,
    AppliedDraftMarker draftMarker
) : IRequestHandler<ApplyMetricDefinitionChangesRequest, ApplyMetricDefinitionChangesResponse>
{
    private readonly AppliedDraftMarker _draftMarker = draftMarker;
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IMetricSchemaGenerator _metricSchemaGenerator = metricSchemaGenerator;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<ApplyMetricDefinitionChangesResponse> HandleAsync(
        ApplyMetricDefinitionChangesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var registration = await _dbContext.MachineRegistrations
                .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken)
            ?? throw new NotFoundException(MachineRegistrationErrors.MachineNotFound);

        if (registration.MetricSchemaRevision != request.ExpectedRevision)
        {
            throw new ConflictException(MetricDefinitionErrors.RevisionStale);
        }

        var definitions = await _dbContext.MetricDefinitions
            .Include(metric => metric.MeasurementUnit)
            .ThenInclude(unit => unit.QuantityKind)
            .Where(metric => metric.MachineRegistrationId == registration.Id)
            .ToListAsync(cancellationToken);

        var units = await _dbContext.MeasurementUnits
            .AsNoTracking()
            .Include(unit => unit.QuantityKind)
            .ToListAsync(cancellationToken);

        var changedIds = request.Updated.Select(metric => metric.Id).Concat(request.Deleted).ToList();
        var unknownId = changedIds.Except(definitions.Select(metric => metric.Id)).Any();

        if (unknownId)
        {
            throw new NotFoundException(MetricDefinitionErrors.MetricNotFound);
        }

        var takenKeys = definitions
            .Where(metric => !request.Deleted.Contains(metric.Id))
            .Select(metric => metric.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (request.Created.Any(metric => takenKeys.Contains(metric.Key)))
        {
            throw new ConflictException(MetricDefinitionErrors.KeyTaken);
        }

        var created = request.Created
            .Select((metric, index) => CreateDefinition(registration.Id, metric, index, units))
            .ToList();

        var updateResults = request.Updated
            .Select((updated, index) => UpdateDefinition(
                definitions.Single(metric => metric.Id == updated.Id),
                updated,
                index))
            .ToList();

        var isSchemaChanged = created.Count > 0 || request.Deleted.Count > 0 || updateResults.Contains(item: true);

        var deleted = definitions.Where(metric => request.Deleted.Contains(metric.Id)).ToList();
        await RemoveFromPhasesAsync(registration.Id, request.Deleted, cancellationToken);
        _dbContext.MetricDefinitions.RemoveRange(deleted);
        _dbContext.MetricDefinitions.AddRange(created);

        if (isSchemaChanged)
        {
            var revision = registration.AdvanceMetricSchemaRevision();
            var active = definitions.Except(deleted).Concat(created).ToList();
            var schemaMetrics = active.Select(metric => ToSchemaMetric(metric, units)).ToList();

            _dbContext.MetricSchemaRevisions.Add(
                new MetricSchemaRevision(
                    registration.Id,
                    revision,
                    _metricSchemaGenerator.Generate(registration.GatewayId, revision, schemaMetrics),
                    _timeProvider.GetUtcNow().UtcDateTime));
        }

        await _draftMarker.MarkMetricDraftsAsync(
            request.CompanyId,
            request.MachineId,
            [
                .. request.Created
                    .Select((metric, index) => (metric.DraftId, Path: $"created[{index}].draftId", created[index].Id))
                    .Concat(
                        request.Updated.Select((metric, index) =>
                            (metric.DraftId, Path: $"updated[{index}].draftId", metric.Id)))
                    .Where(reference => reference.DraftId is not null)
                    .Select(reference => new DraftReference
                    {
                        Path = reference.Path,
                        DraftId = reference.DraftId!.Value,
                        DefinitionId = reference.Id,
                    }),
            ],
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is UniqueConstraintException or DbUpdateConcurrencyException)
        {
            throw new ConflictException(MetricDefinitionErrors.RevisionStale, exception);
        }

        return new ApplyMetricDefinitionChangesResponse { Revision = registration.MetricSchemaRevision };
    }

    private static MetricDefinition CreateDefinition(
        Guid registrationId,
        CreatedMetricDefinition metric,
        int index,
        IReadOnlyList<MeasurementUnit> units
    )
    {
        var unit = ResolveUnit(metric, index, units);

        var definition = new MetricDefinition(
            registrationId,
            metric.Key,
            metric.Name,
            metric.Description,
            metric.ValueKind!,
            unit,
            metric.Min,
            metric.Max,
            metric.States);

        definition.ChangeLimits(metric.ToLimits(), metric.Behavior ?? MetricBehavior.Level);

        return definition;
    }

    private static MeasurementUnit ResolveUnit(
        CreatedMetricDefinition metric,
        int index,
        IReadOnlyList<MeasurementUnit> units
    )
    {
        if (metric.ValueKind != MetricValueKind.Numeric)
        {
            var discreteName = metric.ValueKind == MetricValueKind.Boolean
                ? DiscreteUnits.BooleanName
                : DiscreteUnits.StateName;

            return units.Single(unit => DiscreteUnits.IsDiscrete(unit) && unit.Name == discreteName);
        }

        var reference = metric.Unit!;

        var found = units.FirstOrDefault(unit =>
                unit.QuantityKind.Name == reference.QuantityKind
                && unit.Name == reference.Name)
            ?? throw Rejected($"created[{index}].unit", MetricDefinitionErrors.UnitUnknown);

        return DiscreteUnits.IsDiscrete(found)
            ? throw Rejected($"created[{index}].unit", MetricDefinitionErrors.UnitDiscreteNotAllowed)
            : found;
    }

    private static bool UpdateDefinition(MetricDefinition definition, UpdatedMetricDefinition updated, int index)
    {
        if (definition.ValueKind != MetricValueKind.Numeric && (updated.Min is not null || updated.Max is not null))
        {
            throw Rejected($"updated[{index}].min", MetricDefinitionErrors.RangeNotAllowed);
        }

        if (definition.ValueKind == MetricValueKind.StateSet && updated.States is null)
        {
            throw Rejected($"updated[{index}].states", MetricDefinitionErrors.StatesRequired);
        }

        if (definition.ValueKind != MetricValueKind.StateSet && updated.States is not null)
        {
            throw Rejected($"updated[{index}].states", MetricDefinitionErrors.StatesNotAllowed);
        }

        if (definition.ValueKind != MetricValueKind.Numeric && updated.HasAnyLimit)
        {
            throw Rejected($"updated[{index}].warningLow", MetricDefinitionErrors.LimitsNotAllowed);
        }

        var isChanged = definition.Update(updated.Name, updated.Description, updated.Min, updated.Max, updated.States);
        definition.ChangeLimits(updated.ToLimits(), updated.Behavior ?? MetricBehavior.Level);

        return isChanged;
    }

    private async Task RemoveFromPhasesAsync(
        Guid registrationId,
        IReadOnlyList<Guid> deletedIds,
        CancellationToken cancellationToken
    )
    {
        if (deletedIds.Count == 0)
        {
            return;
        }

        var phases = await _dbContext.OperatingPhases
            .Include(phase => phase.Conditions)
            .Include(phase => phase.Limits)
            .Where(phase => phase.MachineRegistrationId == registrationId)
            .ToListAsync(cancellationToken);

        if (phases.Any(phase => phase.UsesAnyMetricInConditions(deletedIds)))
        {
            throw new UnprocessableException(MetricDefinitionErrors.UsedByPhase);
        }

        foreach (var phase in phases)
        {
            phase.RemoveLimitsOf(deletedIds);
        }
    }

    private static MetricSchema ToSchemaMetric(MetricDefinition metric, IReadOnlyList<MeasurementUnit> units)
    {
        var unit = units.Single(candidate => candidate.Id == metric.MeasurementUnitId);

        return new MetricSchema
        {
            Key = metric.Key,
            Name = metric.Name,
            ValueKind = metric.ValueKind,
            UnitQuantity = unit.QuantityKind.Name,
            UnitName = unit.Name,
            Min = metric.Min,
            Max = metric.Max,
            States = metric.States,
        };
    }

    private static AppValidationException Rejected(string path, string message) =>
        new([new ValidationFailure(path, message)]);
}
