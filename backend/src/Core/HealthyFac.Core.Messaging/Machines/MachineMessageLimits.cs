namespace HealthyFac.Core.Messaging.Machines;

/// <summary>
/// Defines the limits of the machine data that the Core messages carry. The message validators and the Core
/// Application limits (<c>MachineValidationConstants</c>) use the same values.
/// </summary>
public static class MachineMessageLimits
{
    public const int NameMaxLength = 200;
}
