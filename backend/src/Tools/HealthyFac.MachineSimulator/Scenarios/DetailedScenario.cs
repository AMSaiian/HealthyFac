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
                failedModules.SelectMany(module => module.FailureCodes).Distinct().ToList(),
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
                ? items[index: 0].effect.Curve.ValueAt(items[index: 0].Progress)
                : (double)values[group.Key] + items.Sum(item => item.effect.Curve.OffsetAt(item.Progress));
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

        return codes.Distinct().ToList();
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
