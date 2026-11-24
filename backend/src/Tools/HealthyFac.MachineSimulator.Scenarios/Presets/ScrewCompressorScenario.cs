using HealthyFac.MachineSimulator.Scenarios.Curves;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Scenarios.Modules.Effects;

namespace HealthyFac.MachineSimulator.Scenarios.Presets;

/// <summary>
/// The screw compressor (serial number <c>NRS37-22-1093</c>): cycles of 120 seconds loaded and 30 seconds standby. The
/// oil filter, the air filter, and the condensate drain are warning modules. The other modules are trip modules.
/// </summary>
public sealed class ScrewCompressorScenario : IMachineScenario
{
    private const double LoadedSeconds = 120;
    private const double CycleSeconds = 150;
    private const string LoadedCode = "I0002";
    private const string StandbyCode = "I0001";
    private const string LoadedState = "loaded";
    private const string StandbyState = "standby";
    private const string StoppedState = "stopped";
    private const string FaultState = "fault";
    private const double MinSpeed = 1800;
    private const double MaxSpeed = 3200;
    private const double SpeedMiddle = 2500;
    private const double SpeedSwing = 700;
    private const double SpeedPeriodSeconds = 60;
    private const double LoadedVesselPressure = 7.3;
    private const double StandbyVesselPressure = 4.5;
    private const double ConverterTemperature = 50;
    private const double ElementMiddleTemperature = 84;
    private const double ElementReferenceTemperature = 86;
    private const double OilFilterDrop = 0.4;
    private const double OilSeparatorDrop = 0.3;
    private const double AirFilterDrop = 1.2;
    private const double Ambient = 22;
    private const double DrainFaultProgress = 0.8;
    private const double StandbyEndPressure = 6.8;
    private const double StandbyStartPressure = 7.2;
    private const double StandbySeconds = CycleSeconds - LoadedSeconds;
    private const double MinStoppedPressure = 5.5;
    private const double PressureFallPerSecond = 0.01;

    private static readonly DrainCurve ElementTemperature = DrainCurve.FourPoints(
        ElementReferenceTemperature, normalLimit: 100, alarm: 110, trip: 115);

    private static readonly DrainCurve SeparatorDrop = DrainCurve.FourPoints(
        OilSeparatorDrop, normalLimit: 0.6, alarm: 0.8, trip: 1.0);

    private static readonly DrainCurve OilDrop = DrainCurve.FourPoints(
        OilFilterDrop, normalLimit: 1.0, alarm: 1.5, trip: 1.8);

    private static readonly DrainCurve AirDrop = DrainCurve.FourPoints(
        AirFilterDrop, normalLimit: 3.0, alarm: 5.0, trip: 6.0);

    private static readonly DrainCurve ConverterCurve = DrainCurve.FourPoints(
        ConverterTemperature, normalLimit: 70, alarm: 80, trip: 90);

    private static readonly DetailedScenario Engine = new(CreateSpec());

    public ScenarioKind Kind => Engine.Kind;

    public IReadOnlyList<ModuleDefinition> Modules => Engine.Modules;

    public IReadOnlyDictionary<string, double> CounterStartValues => Engine.CounterStartValues;

    public ScenarioSample Sample(ScenarioContext context) => Engine.Sample(context);

    private static ScenarioSpec CreateSpec() =>
        new()
        {
            Kind = ScenarioKind.ScrewCompressor,
            CounterStartValues = new Dictionary<string, double>
            {
                [Keys.RunningHours] = 14010,
                [Keys.LoadedHours] = 11860,
            },
            Running = Run,
            Stopped = Stop,
            FaultState = (Keys.State, FaultState),
            Modules =
            [
                new ModuleDefinition
                {
                    Name = ModuleName.OilCooler, Kind = ModuleKind.Trip, FailureCodes = ["S2011"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.ElementTemperature, Curve = ElementTemperature },
                        new RelativeEffect { Key = Keys.MotorPower, ShareAtEnd = 0.1 },
                    ],
                    DrainCodes = [CodeRule.WhileAbove(code: "W2010", ElementTemperature, limit: 110)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.OilSeparator, Kind = ModuleKind.Trip, FailureCodes = ["S3080"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.OilSeparatorDrop, Curve = SeparatorDrop },
                        new AbsoluteEffect
                        {
                            Key = Keys.VesselPressure,
                            Curve = DrainCurve.FourPoints(
                                LoadedVesselPressure,
                                normalLimit: 8.0,
                                alarm: 8.3,
                                trip: 8.5),
                        },
                    ],
                    DrainCodes = [CodeRule.WhileAbove(code: "W3050", SeparatorDrop, limit: 0.8)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.OilFilter, Kind = ModuleKind.Warning, FailureCodes = ["W3070"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.OilFilterDrop, Curve = OilDrop },
                        new OffsetEffect { Key = Keys.ElementTemperature, AtEnd = 3 },
                    ],
                    DrainCodes = [CodeRule.WhileAbove(code: "W3070", OilDrop, limit: 1.5)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.AirFilter, Kind = ModuleKind.Warning, FailureCodes = ["W3060"], Effects =
                    [
                        new AbsoluteEffect { Key = Keys.AirFilterDrop, Curve = AirDrop },
                        new RelativeEffect { Key = Keys.MotorPower, ShareAtEnd = 0.05 },
                    ],
                    DrainCodes = [CodeRule.WhileAbove(code: "W3060", AirDrop, limit: 5.0)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.Converter, Kind = ModuleKind.Trip, FailureCodes = ["S4020"],
                    Effects = [new AbsoluteEffect { Key = Keys.ConverterTemperature, Curve = ConverterCurve }],
                    DrainCodes = [CodeRule.WhileAbove(code: "W4030", ConverterCurve, limit: 80)],
                },
                new ModuleDefinition
                {
                    Name = ModuleName.CondensateDrain, Kind = ModuleKind.Warning, FailureCodes = ["W7010"],
                    Effects =
                    [
                        new SwitchEffect { Key = Keys.DrainFault, IsOn = true, FromProgress = DrainFaultProgress },
                    ],
                    DrainCodes = [CodeRule.From(code: "W7010", DrainFaultProgress)],
                },
            ],
        };

    private static PhaseSample Run(ScenarioContext context)
    {
        var position = context.RunTime.TotalSeconds % CycleSeconds;
        var isLoaded = position < LoadedSeconds;

        return isLoaded ? Loaded(context, position) : Standby(context, position - LoadedSeconds);
    }

    private static PhaseSample Loaded(ScenarioContext context, double position)
    {
        var speed = SpeedMiddle + SpeedSwing * Math.Sin(position * 2 * Math.PI / SpeedPeriodSeconds);
        var share = (speed - MinSpeed) / (MaxSpeed - MinSpeed);
        var pressure = 7.0 + 0.1 * Math.Sin(position * 2 * Math.PI / 10);

        var values = new Dictionary<string, object>
        {
            [Keys.State] = LoadedState,
            [Keys.DischargePressure] = SlowCooling.Hold(context, Keys.DischargePressure, pressure),
            [Keys.VesselPressure] = LoadedVesselPressure,
            [Keys.MotorSpeed] = speed,
            [Keys.MotorPower] = 15 + 20 * share,
            [Keys.MotorCurrent] = 25 + 35 * share,
            [Keys.ElementTemperature] = SlowCooling.Hold(
                context,
                Keys.ElementTemperature,
                ElementMiddleTemperature + 4 * Math.Sin(position * 2 * Math.PI / SpeedPeriodSeconds)),
            [Keys.ConverterTemperature] = SlowCooling.Hold(context, Keys.ConverterTemperature, ConverterTemperature),
            [Keys.AmbientTemperature] = Ambient,
            [Keys.OilFilterDrop] = OilFilterDrop,
            [Keys.OilSeparatorDrop] = OilSeparatorDrop,
            [Keys.AirFilterDrop] = AirFilterDrop,
            [Keys.DrainFault] = false,
            [Keys.RunningHours] = context.Counters[Keys.RunningHours],
            [Keys.LoadedHours] = context.Counters[Keys.LoadedHours],
        };

        return new PhaseSample
        {
            Values = values, InfoCode = LoadedCode, IsMain = true,
            GrowingCounters = [Keys.RunningHours, Keys.LoadedHours],
        };
    }

    private static PhaseSample Standby(ScenarioContext context, double position)
    {
        var pressure = StandbyStartPressure - (StandbyStartPressure - StandbyEndPressure) * position / StandbySeconds;

        var values = new Dictionary<string, object>
        {
            [Keys.State] = StandbyState,
            [Keys.DischargePressure] = SlowCooling.Hold(context, Keys.DischargePressure, pressure),
            [Keys.VesselPressure] = StandbyVesselPressure,
            [Keys.MotorSpeed] = 0d,
            [Keys.MotorPower] = 0d,
            [Keys.MotorCurrent] = 0d,
            [Keys.ElementTemperature] = SlowCooling.Cool(context, Keys.ElementTemperature, ElementMiddleTemperature),
            [Keys.ConverterTemperature] = SlowCooling.Hold(context, Keys.ConverterTemperature, value: 40),
            [Keys.AmbientTemperature] = Ambient,
            [Keys.OilFilterDrop] = 0d,
            [Keys.OilSeparatorDrop] = 0d,
            [Keys.AirFilterDrop] = 0d,
            [Keys.DrainFault] = false,
            [Keys.RunningHours] = context.Counters[Keys.RunningHours],
            [Keys.LoadedHours] = context.Counters[Keys.LoadedHours],
        };

        return new PhaseSample
            { Values = values, InfoCode = StandbyCode, IsMain = false, GrowingCounters = [Keys.RunningHours] };
    }

    private static IReadOnlyDictionary<string, object> Stop(ScenarioContext context) =>
        new Dictionary<string, object>
        {
            [Keys.State] = StoppedState,
            [Keys.DischargePressure] = SlowFall.Fall(
                context,
                Keys.DischargePressure,
                StandbyEndPressure,
                MinStoppedPressure,
                PressureFallPerSecond),
            [Keys.VesselPressure] = 0d,
            [Keys.MotorSpeed] = 0d,
            [Keys.MotorPower] = 0d,
            [Keys.MotorCurrent] = 0d,
            [Keys.ElementTemperature] = SlowCooling.Cool(context, Keys.ElementTemperature, ElementMiddleTemperature),
            [Keys.ConverterTemperature] = SlowCooling.Cool(context, Keys.ConverterTemperature, ConverterTemperature),
            [Keys.AmbientTemperature] = Ambient,
            [Keys.OilFilterDrop] = 0d,
            [Keys.OilSeparatorDrop] = 0d,
            [Keys.AirFilterDrop] = 0d,
            [Keys.DrainFault] = context.Modules[ModuleName.CondensateDrain].Progress >= DrainFaultProgress,
            [Keys.RunningHours] = context.Counters[Keys.RunningHours],
            [Keys.LoadedHours] = context.Counters[Keys.LoadedHours],
        };

    private static class Keys
    {
        public const string State = "compressor.state";
        public const string DischargePressure = "discharge.pressure";
        public const string VesselPressure = "vessel.pressure";
        public const string MotorSpeed = "motor.speed";
        public const string MotorPower = "motor.power";
        public const string MotorCurrent = "motor.current";
        public const string ElementTemperature = "element.outlet_temperature";
        public const string ConverterTemperature = "converter.temperature";
        public const string AmbientTemperature = "ambient.temperature";
        public const string OilFilterDrop = "oil_filter.pressure_drop";
        public const string OilSeparatorDrop = "oil_separator.pressure_drop";
        public const string AirFilterDrop = "air_filter.pressure_drop";
        public const string DrainFault = "drain.fault";
        public const string RunningHours = "running.hours";
        public const string LoadedHours = "loaded.hours";
    }
}
