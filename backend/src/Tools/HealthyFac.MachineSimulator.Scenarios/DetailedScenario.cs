using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

namespace HealthyFac.MachineSimulator.Scenarios;

/// <summary>
/// Turns the data of a <see cref="ScenarioSpec"/> into samples. The rules are the same for every detailed scenario:
/// the maintenance state and a trip failure show stopped values, a drained value replaces the value of the main phase
/// and shifts the value of the other phases, and a warning failure keeps the values at their trip point.
/// </summary>
/// <param name="spec">The data of the scenario.</param>
public sealed class DetailedScenario(ScenarioSpec spec) : IMachineScenario
{
    private readonly ScenarioSpec _spec = spec;

    public ScenarioKind Kind => _spec.Kind;

    public IReadOnlyList<ModuleDefinition> Modules => _spec.Modules;

    public IReadOnlyDictionary<string, double> CounterStartValues => _spec.CounterStartValues;

    public ScenarioSample Sample(ScenarioContext context)
    {
        if (context.IsStopped)
        {
            return CreateStopped(context, [], isFault: false);
        }

        var failedModules = _spec.Modules.Where(module => IsFailure(context, module)).ToList();

        if (failedModules.Any(module => module.Kind == ModuleKind.Trip))
        {
            return CreateStopped(
                context,
                [.. failedModules.SelectMany(module => module.FailureCodes).Distinct()],
                isFault: true);
        }

        var phase = _spec.Running(context);
        var values = phase.Values.ToDictionary(pair => pair.Key, pair => pair.Value);
        ApplyEffects(context, values, phase.IsMain);

        return new ScenarioSample
        {
            Values = AddNoise(values, context.Random),
            StatusCodes = CollectCodes(context, phase.InfoCode),
            IsRunning = true,
            GrowingCounters = phase.GrowingCounters,
        };
    }

    private ScenarioSample CreateStopped(ScenarioContext context, IReadOnlyList<string> codes, bool isFault)
    {
        var values = _spec.Stopped(context).ToDictionary(pair => pair.Key, pair => pair.Value);

        if (isFault && _spec.FaultState is { } fault)
        {
            values[fault.Key] = fault.Value;
        }

        return new ScenarioSample
        {
            Values = AddNoise(values, context.Random),
            StatusCodes = codes,
            IsRunning = false,
        };
    }

    private static bool IsFailure(ScenarioContext context, ModuleDefinition module) =>
        context.Modules.TryGetValue(module.Name, out var state) && state.Mode == ModuleMode.Failure;

    private void ApplyEffects(ScenarioContext context, Dictionary<string, object> values, bool isMainPhase)
    {
        var active = _spec.Modules
            .Where(module => context.Modules.TryGetValue(module.Name, out var state) && state.Progress > 0)
            .Select(module => (Module: module, Progress: context.Modules[module.Name].Progress))
            .ToList();

        var absolute = active
            .SelectMany(item => item.Module.Effects.OfType<AbsoluteEffect>().Select(effect => (effect, item.Progress)))
            .GroupBy(item => item.effect.Key);

        foreach (var group in absolute)
        {
            var items = group.ToList();

            values[group.Key] = items.Count == 1 && isMainPhase
                ? items[0].effect.Curve.ValueAt(items[0].Progress)
                : ClampToCurve(
                    (double)values[group.Key] + items.Sum(item => item.effect.Curve.OffsetAt(item.Progress)),
                    items,
                    isMainPhase);
        }

        foreach (var (module, progress) in active)
        {
            foreach (var effect in module.Effects)
            {
                switch (effect)
                {
                    case OffsetEffect offset:
                        values[offset.Key] = (double)values[offset.Key] + offset.AtEnd * progress;

                        break;
                    case RelativeEffect relative:
                        values[relative.Key] = (double)values[relative.Key] * (1 + relative.ShareAtEnd * progress);

                        break;
                    case SwitchEffect toggle when progress >= toggle.FromProgress:
                        values[toggle.Key] = toggle.IsOn;

                        break;
                }
            }
        }
    }

    /// <summary>
    /// Keeps a drained value of a phase other than the main phase on the drain side of its curve: the phase value is
    /// not the reference of the curve, so the sum could pass the trip value that the curve reaches at the same
    /// progress. The effect with the largest offset gives the curve. The main phase keeps the sum of the offsets.
    /// </summary>
    /// <param name="value">The phase value plus the offsets of the effects.</param>
    /// <param name="items">The effects of one key with the progress of their modules.</param>
    /// <param name="isMainPhase">Whether the sample belongs to the main phase.</param>
    /// <returns>The value, limited by the curve.</returns>
    private static double ClampToCurve(
        double value,
        List<(AbsoluteEffect effect, double Progress)> items,
        bool isMainPhase
    )
    {
        if (isMainPhase)
        {
            return value;
        }

        var (effect, progress) = items.MaxBy(item => Math.Abs(item.effect.Curve.OffsetAt(item.Progress)));
        var curveValue = effect.Curve.ValueAt(progress);

        return effect.Curve.Trip >= effect.Curve.Reference
            ? Math.Min(value, curveValue)
            : Math.Max(value, curveValue);
    }

    private List<string> CollectCodes(ScenarioContext context, string? infoCode)
    {
        List<string> codes = infoCode is null ? [] : [infoCode];

        foreach (var module in _spec.Modules)
        {
            if (!context.Modules.TryGetValue(module.Name, out var state) || state.Mode == ModuleMode.Expected)
            {
                continue;
            }

            codes.AddRange(module.DrainCodes.Where(rule => rule.IsActive(state.Progress)).Select(rule => rule.Code));

            if (state.Mode == ModuleMode.Failure)
            {
                codes.AddRange(module.FailureCodes);
            }
        }

        return [.. codes.Distinct()];
    }

    private Dictionary<string, object> AddNoise(Dictionary<string, object> values, Random random)
    {
        foreach (var (key, value) in values.ToList())
        {
            if (value is double number && !_spec.CounterStartValues.ContainsKey(key))
            {
                values[key] = ValueNoise.Apply(number, random);
            }
        }

        return values;
    }
}
