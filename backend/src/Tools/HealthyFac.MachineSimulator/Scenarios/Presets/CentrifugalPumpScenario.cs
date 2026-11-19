using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

namespace HealthyFac.MachineSimulator.Scenarios.Presets;

/// <summary>
/// The centrifugal pump (serial number <c>HV-EP80-2023-04412</c>): continuous duty. The seal is a warning module, and
/// every other module is a trip module.
/// </summary>
public sealed class CentrifugalPumpScenario : IMachineScenario
{
    private const string RunningCode = "P-22";
    private const string RunningState = "running";
    private const string StoppedState = "stopped";
    private const string FaultState = "fault";
    private const double Flow = 108;
    private const double DischargePressure = 4.6;
    private const double SuctionPressure = 1.1;
    private const double MotorCurrent = 31.2;
    private const double MotorSpeed = 2950;
    private const double Vibration = 1.6;
    private const double StoppedPressure = 1.1;
    private const double LiquidTemperature = 30;
    private const double BearingDeTemperature = 54;
    private const double BearingNdeTemperature = 49;
    private const double LeakageProgress = 0.8;

    private static readonly DrainCurve BearingVibration = DrainCurve.FourPoints(
        Vibration, normalLimit: 2.8, alarm: 4.5, trip: 7.1);

    private static readonly DrainCurve BearingDe = DrainCurve.FourPoints(
        BearingDeTemperature, normalLimit: 80, alarm: 90, trip: 100);

    private static readonly DrainCurve SuctionCurve = DrainCurve.FourPoints(
        SuctionPressure, normalLimit: 0.5, alarm: 0.4, trip: 0.3);

    private static readonly DrainCurve LowFlow = DrainCurve.FourPoints(Flow, normalLimit: 60, alarm: 45, trip: 35);

    private static readonly DrainCurve DischargeCurve = DrainCurve.FourPoints(
        DischargePressure, normalLimit: 5.0, alarm: 6.5, trip: 8.0);

    private static readonly DetailedScenario Engine = new(CreateSpec());

    public ScenarioKind Kind => Engine.Kind;

    public IReadOnlyList<ModuleDefinition> Modules => Engine.Modules;

    public IReadOnlyDictionary<string, double> CounterStartValues => Engine.CounterStartValues;

    public ScenarioSample Sample(ScenarioContext context) => Engine.Sample(context);

    private static ScenarioSpec CreateSpec() =>
        new()
        {
            Kind = ScenarioKind.CentrifugalPump,
            CounterStartValues = new Dictionary<string, double> { [Keys.OperatingHours] = 12030 },
            Running = Run,
            Stopped = Stop,
            FaultState = (Keys.State, FaultState),
            Modules =
            [
                new ModuleDefinition
                {
                    Name = ModuleName.Bearings, Kind = ModuleKind.Trip, FailureCodes = ["P-05", "P-07"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.BearingDeTemperature, Curve = BearingDe },
                        new AbsoluteEffect { Key = Keys.Vibration, Curve = BearingVibration },
                        new AbsoluteEffect
                        {
                            Key = Keys.BearingNdeTemperature,
                            Curve = DrainCurve.TwoPoints(BearingNdeTemperature, end: 60),
                        },
                        new AbsoluteEffect
                            { Key = Keys.MotorCurrent, Curve = DrainCurve.TwoPoints(MotorCurrent, end: 33.4) },
                    ],
                    DrainCodes =
                    [
                        CodeRule.WhileAbove(code: "P-06", BearingVibration, limit: 4.5),
                        CodeRule.WhileAbove(code: "P-04", BearingDe, limit: 90),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Seal, Kind = ModuleKind.Warning, FailureCodes = ["P-08"],
                    Effects =
                        [new SwitchEffect { Key = Keys.SealLeakage, IsOn = true, FromProgress = LeakageProgress }],
                    DrainCodes = [CodeRule.From(code: "P-08", LeakageProgress)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Suction, Kind = ModuleKind.Trip, FailureCodes = ["P-01"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.SuctionPressure, Curve = SuctionCurve },
                        new AbsoluteEffect { Key = Keys.Flow, Curve = LowFlow },
                        new OffsetEffect { Key = Keys.Vibration, AtEnd = 1.0 },
                        new AbsoluteEffect
                            { Key = Keys.MotorCurrent, Curve = DrainCurve.TwoPoints(MotorCurrent, end: 26) },
                    ],
                    DrainCodes =
                    [
                        CodeRule.WhileBelow(code: "P-02", SuctionCurve, limit: 0.4),
                        CodeRule.WhileBelow(code: "P-03", LowFlow, limit: 45),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Discharge, Kind = ModuleKind.Trip, FailureCodes = ["P-12"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.DischargePressure, Curve = DischargeCurve },
                        new AbsoluteEffect { Key = Keys.Flow, Curve = LowFlow },
                    ],
                    DrainCodes =
                    [
                        CodeRule.WhileAbove(code: "P-11", DischargeCurve, limit: 6.5),
                        CodeRule.WhileBelow(code: "P-03", LowFlow, limit: 45),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Motor, Kind = ModuleKind.Trip, FailureCodes = ["P-09"], Effects =
                    [
                        new AbsoluteEffect
                        {
                            Key = Keys.MotorCurrent,
                            Curve = DrainCurve.FourPoints(MotorCurrent, normalLimit: 33.5, alarm: 35, trip: 37),
                        },
                        new AbsoluteEffect
                            { Key = Keys.MotorSpeed, Curve = DrainCurve.TwoPoints(MotorSpeed, end: 2900) },
                        new OffsetEffect { Key = Keys.Vibration, AtEnd = 0.8 },
                    ],
                    DrainCodes = [],
                },
            ],
        };

    private static PhaseSample Run(ScenarioContext context)
    {
        var values = new Dictionary<string, object>
        {
            [Keys.State] = RunningState,
            [Keys.Flow] = Flow,
            [Keys.DischargePressure] = DischargePressure,
            [Keys.SuctionPressure] = SuctionPressure,
            [Keys.MotorCurrent] = MotorCurrent,
            [Keys.MotorSpeed] = MotorSpeed,
            [Keys.BearingDeTemperature] = SlowCooling.Hold(context, Keys.BearingDeTemperature, BearingDeTemperature),
            [Keys.BearingNdeTemperature] = SlowCooling.Hold(context, Keys.BearingNdeTemperature, BearingNdeTemperature),
            [Keys.Vibration] = Vibration,
            [Keys.LiquidTemperature] = LiquidTemperature,
            [Keys.SealLeakage] = false,
            [Keys.OperatingHours] = context.Counters[Keys.OperatingHours],
        };

        return new PhaseSample
            { Values = values, InfoCode = RunningCode, IsMain = true, GrowingCounters = [Keys.OperatingHours] };
    }

    private static IReadOnlyDictionary<string, object> Stop(ScenarioContext context) =>
        new Dictionary<string, object>
        {
            [Keys.State] = StoppedState,
            [Keys.Flow] = 0d,
            [Keys.DischargePressure] = StoppedPressure,
            [Keys.SuctionPressure] = StoppedPressure,
            [Keys.MotorCurrent] = 0d,
            [Keys.MotorSpeed] = 0d,
            [Keys.BearingDeTemperature] = SlowCooling.Cool(context, Keys.BearingDeTemperature, BearingDeTemperature),
            [Keys.BearingNdeTemperature] = SlowCooling.Cool(context, Keys.BearingNdeTemperature, BearingNdeTemperature),
            [Keys.Vibration] = 0d,
            [Keys.LiquidTemperature] = LiquidTemperature,
            [Keys.SealLeakage] = context.Modules[ModuleName.Seal].Progress >= LeakageProgress,
            [Keys.OperatingHours] = context.Counters[Keys.OperatingHours],
        };

    private static class Keys
    {
        public const string State = "pump.state";
        public const string Flow = "flow.rate";
        public const string DischargePressure = "discharge.pressure";
        public const string SuctionPressure = "suction.pressure";
        public const string MotorCurrent = "motor.current";
        public const string MotorSpeed = "motor.speed";
        public const string BearingDeTemperature = "bearing_de.temperature";
        public const string BearingNdeTemperature = "bearing_nde.temperature";
        public const string Vibration = "pump.vibration";
        public const string LiquidTemperature = "liquid.temperature";
        public const string SealLeakage = "seal.leakage";
        public const string OperatingHours = "pump.operating_hours";
    }
}
