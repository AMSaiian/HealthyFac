using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Domain.OperatingPhases;

/// <summary>
/// A named operating phase of a machine, for example <c>cutting</c>. The first phase of a machine whose conditions
/// all hold, in the order of the priority, is the phase of a sample. The phase limit rows replace the limits of the
/// metric definitions inside the phase. A removal keeps the row (soft delete).
/// </summary>
public class OperatingPhase : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private readonly List<OperatingPhaseCondition> _conditions = [];
    private readonly List<OperatingPhaseLimit> _limits = [];

    /// <summary>
    /// Creates a phase.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine. It never changes.</param>
    /// <param name="code">The key of the phase.</param>
    /// <param name="name">The name as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="priority">The order of the phase. A lower number is checked first.</param>
    /// <param name="isTracked">Tells whether the samples of the phase teach the statistics.</param>
    /// <param name="conditions">At least 1 condition.</param>
    /// <param name="limits">The limit rows, one for each metric.</param>
    /// <exception cref="InvalidOperationException">The code is not a valid key, the priority is negative, there is
    /// no condition, or the limit rows break a rule.</exception>
    public OperatingPhase(
        Guid machineRegistrationId,
        string code,
        string name,
        string? description,
        int priority,
        bool isTracked,
        IReadOnlyList<OperatingPhaseCondition> conditions,
        IReadOnlyList<OperatingPhaseLimit> limits
    )
    {
        MachineRegistrationId = machineRegistrationId;
        Code = EnsureCode(code);
        Name = name;
        Description = description;
        Priority = EnsurePriority(priority);
        IsTracked = isTracked;
        EnsureConditions(conditions);
        EnsureLimits(limits, conditions);
        _conditions.AddRange(conditions);
        _limits.AddRange(limits);
    }

    private OperatingPhase()
    {
        Code = null!;
        Name = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
    public Guid MachineRegistrationId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }

    /// <summary>The description as HTML. The Application layer checks it against the rich text policy.</summary>
    public string? Description { get; private set; }

    public int Priority { get; private set; }

    /// <summary>Tells whether the samples of the phase teach the statistics.</summary>
    public bool IsTracked { get; private set; }

    public IReadOnlyList<OperatingPhaseCondition> Conditions => _conditions;
    public IReadOnlyList<OperatingPhaseLimit> Limits => _limits;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits, except the conditions, the limits, and the priority.
    /// </summary>
    /// <param name="name">The name as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="isTracked">Tells whether the samples of the phase teach the statistics.</param>
    /// <returns><see langword="true"/> when <paramref name="isTracked"/> changed. The caller then resets the
    /// statistics of the phase.</returns>
    public bool Update(string name, string? description, bool isTracked)
    {
        var isTrackedChanged = IsTracked != isTracked;
        Name = name;
        Description = description;
        IsTracked = isTracked;

        return isTrackedChanged;
    }

    /// <summary>
    /// Changes the key of the phase.
    /// </summary>
    /// <param name="code">The new key.</param>
    /// <exception cref="InvalidOperationException">The code is not a valid key.</exception>
    public void ChangeCode(string code) => Code = EnsureCode(code);

    /// <summary>
    /// Changes the order of the phase.
    /// </summary>
    /// <param name="priority">The new priority. A lower number is checked first.</param>
    /// <exception cref="InvalidOperationException">The priority is negative.</exception>
    public void ChangePriority(int priority) => Priority = EnsurePriority(priority);

    /// <summary>
    /// Replaces all conditions of the phase.
    /// </summary>
    /// <param name="conditions">At least 1 condition. No condition may use a metric of a limit row.</param>
    /// <returns><see langword="true"/> when the new conditions differ from the stored ones. The caller then resets
    /// the statistics of the phase.</returns>
    /// <exception cref="InvalidOperationException">There is no condition, or a condition uses a metric of a limit
    /// row.</exception>
    public bool ReplaceConditions(IReadOnlyList<OperatingPhaseCondition> conditions)
    {
        EnsureConditions(conditions);
        EnsureLimits(_limits, conditions);

        return ApplyConditions(conditions);
    }

    /// <summary>
    /// Replaces all conditions and all limit rows in one step. The pair is valid as a whole, so a new condition may
    /// use a metric whose old limit row the new rows no longer hold.
    /// </summary>
    /// <param name="conditions">At least 1 condition.</param>
    /// <param name="limits">The rows, one for each metric. No row may use a metric of a condition.</param>
    /// <returns><see langword="true"/> when the new conditions differ from the stored ones. The caller then resets
    /// the statistics of the phase.</returns>
    /// <exception cref="InvalidOperationException">There is no condition, a metric has more than 1 row, or a row
    /// uses a metric of a condition.</exception>
    public bool Replace(
        IReadOnlyList<OperatingPhaseCondition> conditions,
        IReadOnlyList<OperatingPhaseLimit> limits
    )
    {
        EnsureConditions(conditions);
        EnsureLimits(limits, conditions);
        var isChanged = ApplyConditions(conditions);
        ApplyLimits(limits);

        return isChanged;
    }

    /// <summary>
    /// Replaces all limit rows of the phase. A change of the rows does not reset the statistics.
    /// </summary>
    /// <param name="limits">The rows, one for each metric. No row may use a metric of a condition.</param>
    /// <exception cref="InvalidOperationException">A metric has more than 1 row, or a row uses a metric of a
    /// condition.</exception>
    public void ReplaceLimits(IReadOnlyList<OperatingPhaseLimit> limits)
    {
        EnsureLimits(limits, _conditions);
        ApplyLimits(limits);
    }

    /// <summary>
    /// Tells whether a condition of the phase reads one of the metrics.
    /// </summary>
    /// <param name="metricIds">The metric definitions to look for.</param>
    /// <returns><see langword="true"/> when a condition uses one of the metrics.</returns>
    public bool UsesAnyMetricInConditions(IReadOnlyCollection<Guid> metricIds) =>
        _conditions.Any(condition =>
            condition.MetricDefinitionId is { } metricId && metricIds.Contains(metricId));

    /// <summary>
    /// Removes the limit rows of the metrics. The rows of the other metrics stay. A removal does not reset the
    /// statistics.
    /// </summary>
    /// <param name="metricIds">The metric definitions that leave the machine.</param>
    public void RemoveLimitsOf(IReadOnlyCollection<Guid> metricIds) =>
        _limits.RemoveAll(limit => metricIds.Contains(limit.MetricDefinitionId));

    private bool ApplyConditions(IReadOnlyList<OperatingPhaseCondition> conditions)
    {
        var isChanged = conditions.Count != _conditions.Count
            || conditions.Any(condition => !_conditions.Any(stored => stored.IsSameAs(condition)));

        _conditions.Clear();
        _conditions.AddRange(conditions);

        return isChanged;
    }

    private void ApplyLimits(IReadOnlyList<OperatingPhaseLimit> limits)
    {
        _limits.RemoveAll(stored => limits.All(limit => limit.MetricDefinitionId != stored.MetricDefinitionId));

        foreach (var limit in limits)
        {
            var existing = _limits.Find(stored => stored.MetricDefinitionId == limit.MetricDefinitionId);

            if (existing is null)
            {
                _limits.Add(limit);
            }
            else
            {
                existing.ChangeLimits(limit.Limits);
            }
        }
    }

    private static string EnsureCode(string code) =>
        MetricDefinitionRules.IsValidKey(code) ? code : throw OperatingPhaseInvariantErrors.InvalidCode(code);

    private static int EnsurePriority(int priority) =>
        priority >= 0 ? priority : throw OperatingPhaseInvariantErrors.NegativePriority(priority);

    private static void EnsureConditions(IReadOnlyList<OperatingPhaseCondition> conditions)
    {
        if (conditions.Count == 0)
        {
            throw OperatingPhaseInvariantErrors.ConditionsRequired();
        }
    }

    private static void EnsureLimits(
        IReadOnlyList<OperatingPhaseLimit> limits,
        IReadOnlyList<OperatingPhaseCondition> conditions
    )
    {
        var duplicate = limits.GroupBy(limit => limit.MetricDefinitionId).FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw OperatingPhaseInvariantErrors.DuplicateLimit(duplicate.Key);
        }

        var conditionMetricIds = conditions
            .Where(condition => condition.MetricDefinitionId is not null)
            .Select(condition => condition.MetricDefinitionId!.Value)
            .ToHashSet();
        var limitOnConditionMetric =
            limits.FirstOrDefault(limit => conditionMetricIds.Contains(limit.MetricDefinitionId));

        if (limitOnConditionMetric is not null)
        {
            throw OperatingPhaseInvariantErrors.LimitOnConditionMetric(limitOnConditionMetric.MetricDefinitionId);
        }
    }
}
