namespace HealthyFac.Telemetry.UnitTests.Intake.Data;

public static class MqttIntakeOptionsValidatorTestData
{
    public static TheoryData<string> InvalidGroups => new() { "a/b", "a+b", "a#", "/", string.Empty };

    public static TheoryData<int, int> LongestValidClientIds =>
        new() { { 1, 254 }, { 2, 254 }, { 10, 254 }, { 11, 253 }, { 16, 253 } };

    public static TheoryData<int, int> TooLongClientIds => new() { { 1, 255 }, { 10, 255 }, { 11, 254 }, { 16, 254 } };

    public static TheoryData<int> ValidDrainTimeouts => new() { 1, 29 };

    public static TheoryData<int> InvalidDrainTimeouts => new() { 0, 30 };
}
