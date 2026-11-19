using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

namespace HealthyFac.MachineSimulator.Scenarios.Presets;

/// <summary>
/// The CNC turning center (serial number <c>MTC420-24-0187</c>): cycles of 40 seconds of cutting and 10 seconds of
/// part change. Every module is a trip module.
/// </summary>
public sealed class CncTurningCenterScenario : IMachineScenario
{
    private const double CuttingSeconds = 40;
    private const double CycleSeconds = 50;
    private const string CuttingCode = "S002";
    private const string PartChangeCode = "S001";
    private const string AutoMode = "auto";
    private const string OffMode = "off";
    private const double HydraulicPressure = 35.5;
    private const double CoolantLevel = 70;
    private const double CoolantTemperature = 24;
    private const double LubricationPressure = 20;
    private const double SpindleSpeed = 3000;

    private static readonly DrainCurve SpindleVibration = DrainCurve.FourPoints(
        reference: 1.1, normalLimit: 2.8, alarm: 4.5, trip: 7.1);

    private static readonly DrainCurve OilTemperature = DrainCurve.FourPoints(
        reference: 38, normalLimit: 50, alarm: 55, trip: 58);

    private static readonly DrainCurve Level = DrainCurve.FourPoints(
        CoolantLevel, normalLimit: 30, alarm: 25, trip: 10);

    private static readonly DrainCurve LiquidTemperature = DrainCurve.FourPoints(
        CoolantTemperature, normalLimit: 30, alarm: 35, trip: 37);

    private static readonly DrainCurve CabinetTemperature = DrainCurve.FourPoints(
        reference: 30, normalLimit: 40, alarm: 45, trip: 50);

    private static readonly DetailedScenario Engine = new(CreateSpec());

    public ScenarioKind Kind => Engine.Kind;

    public IReadOnlyList<ModuleDefinition> Modules => Engine.Modules;

    public IReadOnlyDictionary<string, double> CounterStartValues => Engine.CounterStartValues;

    public ScenarioSample Sample(ScenarioContext context) => Engine.Sample(context);

    private static ScenarioSpec CreateSpec() =>
        new()
        {
            Kind = ScenarioKind.CncTurningCenter,
            CounterStartValues = new Dictionary<string, double> { [Keys.OperatingHours] = 0 },
            Running = Run,
            Stopped = Stop,
            Modules =
            [
                new ModuleDefinition
                {
                    Name = ModuleName.Spindle, Kind = ModuleKind.Trip, FailureCodes = ["A101", "A111"], Effects =
                    [
                        new AbsoluteEffect
                        {
                            Key = Keys.SpindleTemperature,
                            Curve = DrainCurve.FourPoints(reference: 40, normalLimit: 60, alarm: 65, trip: 70),
                        },
                        new AbsoluteEffect { Key = Keys.SpindleVibration, Curve = SpindleVibration },
                        new RelativeEffect { Key = Keys.SpindleLoad, ShareAtEnd = 0.2 },
                    ],
                    DrainCodes =
                    [
                        CodeRule.From(code: "A112", progress: 0.3),
                        CodeRule.WhileAbove(code: "A110", SpindleVibration, limit: 4.5),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Hydraulics, Kind = ModuleKind.Trip, FailureCodes = ["A201"], Effects =
                    [
                        new AbsoluteEffect
                        {
                            Key = Keys.HydraulicPressure,
                            Curve = DrainCurve.FourPoints(HydraulicPressure, normalLimit: 30, alarm: 28, trip: 25),
                        },
                        new AbsoluteEffect { Key = Keys.OilTemperature, Curve = OilTemperature },
                    ],
                    DrainCodes =
                    [
                        CodeRule.From(code: "A205", progress: 0.3),
                        CodeRule.WhileAbove(code: "A203", OilTemperature, limit: 55),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Coolant, Kind = ModuleKind.Trip, FailureCodes = ["A306"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.CoolantLevel, Curve = Level },
                        new AbsoluteEffect { Key = Keys.CoolantTemperature, Curve = LiquidTemperature },
                    ],
                    DrainCodes =
                    [
                        CodeRule.WhileBelow(code: "A305", Level, limit: 25),
                        CodeRule.WhileAbove(code: "A307", LiquidTemperature, limit: 35),
                    ],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Lubrication, Kind = ModuleKind.Trip, FailureCodes = ["A401"], Effects =
                    [
                        new AbsoluteEffect
                        {
                            Key = Keys.LubricationPressure,
                            Curve = DrainCurve.FourPoints(LubricationPressure, normalLimit: 15, alarm: 10, trip: 0),
                        },
                    ],
                    DrainCodes = [CodeRule.From(code: "A402", progress: 0.3)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Cabinet, Kind = ModuleKind.Trip, FailureCodes = ["A802"],
                    Effects = [new AbsoluteEffect { Key = Keys.CabinetTemperature, Curve = CabinetTemperature }],
                    DrainCodes = [CodeRule.WhileAbove(code: "A801", CabinetTemperature, limit: 45)],
                },
            ],
        };

    private static PhaseSample Run(ScenarioContext context)
    {
        var position = context.RunTime.TotalSeconds % CycleSeconds;
        var isCutting = position < CuttingSeconds;

        var values = new Dictionary<string, object>
        {
            [Keys.SpindleMode] = AutoMode,
            [Keys.DoorOpen] = !isCutting,
            [Keys.SpindleSpeed] = isCutting ? SpindleSpeed : 0,
            [Keys.SpindleLoad] = isCutting ? 60 + 20 * Math.Sin(position * 2 * Math.PI / 10) : 0,
            [Keys.SpindleTemperature] = isCutting
                ? SlowCooling.Hold(context, Keys.SpindleTemperature, 36 + 6 * position / CuttingSeconds)
                : SlowCooling.Cool(context, Keys.SpindleTemperature, fallback: 42),
            [Keys.SpindleVibration] = isCutting ? 1.1 : 0.2,
            [Keys.HydraulicPressure] = HydraulicPressure,
            [Keys.OilTemperature] = SlowCooling.Hold(context, Keys.OilTemperature, value: 38),
            [Keys.CoolantLevel] = CoolantLevel,
            [Keys.CoolantTemperature] = CoolantTemperature,
            [Keys.LubricationPressure] = LubricationPressure,
            [Keys.CabinetTemperature] = 30d,
            [Keys.OperatingHours] = context.Counters[Keys.OperatingHours],
        };

        return new PhaseSample
        {
            Values = values, InfoCode = isCutting ? CuttingCode : PartChangeCode, IsMain = isCutting,
            GrowingCounters = [Keys.OperatingHours],
        };
    }

    private static IReadOnlyDictionary<string, object> Stop(ScenarioContext context) =>
        new Dictionary<string, object>
        {
            [Keys.SpindleMode] = OffMode,
            [Keys.DoorOpen] = false,
            [Keys.SpindleSpeed] = 0d,
            [Keys.SpindleLoad] = 0d,
            [Keys.SpindleTemperature] = SlowCooling.Cool(context, Keys.SpindleTemperature, fallback: 42),
            [Keys.SpindleVibration] = 0d,
            [Keys.HydraulicPressure] = HydraulicPressure,
            [Keys.OilTemperature] = SlowCooling.Cool(context, Keys.OilTemperature, fallback: 38),
            [Keys.CoolantLevel] = CoolantLevel,
            [Keys.CoolantTemperature] = CoolantTemperature,
            [Keys.LubricationPressure] = LubricationPressure,
            [Keys.CabinetTemperature] = 28d,
            [Keys.OperatingHours] = context.Counters[Keys.OperatingHours],
        };

    private static class Keys
    {
        public const string SpindleMode = "spindle.mode";
        public const string DoorOpen = "door.open";
        public const string SpindleSpeed = "spindle.speed";
        public const string SpindleLoad = "spindle.load";
        public const string SpindleTemperature = "spindle.temperature";
        public const string SpindleVibration = "spindle.vibration";
        public const string HydraulicPressure = "hydraulic.pressure";
        public const string OilTemperature = "hydraulic.oil_temperature";
        public const string CoolantLevel = "coolant.level";
        public const string CoolantTemperature = "coolant.temperature";
        public const string LubricationPressure = "lubrication.pressure";
        public const string CabinetTemperature = "cabinet.temperature";
        public const string OperatingHours = "machine.operating_hours";
    }
}
