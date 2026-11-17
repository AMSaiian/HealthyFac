using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.MachineSimulator.IntegrationTests.Control.Data;

public static class ControlApiTestData
{
    public const string SpindleModule = "spindle";
    public const string FailureMode = "failure";
    public const string ExpectedMode = "expected";
    public const string UnknownModule = "warp-core";
    public const string SecondSerialNumber = "GEN-0002";

    public static TheoryData<string> AllModules
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var module in ModuleName.All)
            {
                data.Add(module.Value);
            }

            return data;
        }
    }

    public static TheoryData<string, string> InvalidModeBodies =>
        new()
        {
            { "UnknownMode", """{ "mode": "overheated" }""" },
            { "MissingMode", "{}" },
        };

    public static TheoryData<string, string> InvalidRunBodies =>
        new()
        {
            { "MissingFlag", "{}" },
            { "WrongType", """{ "isRunning": "yes" }""" },
        };

    public static string MachineRoute(Guid machineId) => $"/machines/{machineId}";

    public static string RunRoute(Guid machineId) => $"/machines/{machineId}/run";

    public static string ModeRoute(Guid machineId, string module) => $"/machines/{machineId}/modules/{module}/mode";

    public static string RunBody(bool isRunning) =>
        $$"""{ "isRunning": {{isRunning.ToString().ToLowerInvariant()}} }""";

    public static string ModeBody(string mode) => $$"""{ "mode": "{{mode}}" }""";
}
