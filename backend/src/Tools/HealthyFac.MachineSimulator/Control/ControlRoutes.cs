namespace HealthyFac.MachineSimulator.Control;

/// <summary>
/// Names the routes and the route parameters of the control API.
/// </summary>
public static class ControlRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string ModuleParameter = "module";

    public const string MachinesRoute = "/machines";
    public const string MachineRoute = $"{MachinesRoute}/{{{MachineIdParameter}:guid}}";
    public const string RunRoute = $"{MachineRoute}/run";
    public const string ModeRoute = $"{MachineRoute}/modules/{{{ModuleParameter}}}/mode";
}
