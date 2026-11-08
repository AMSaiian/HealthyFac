namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// What the intake did with a received machine message.
/// </summary>
public enum ReceiveMachineMessageResult
{
    /// <summary>The readings are stored and the message has no errors.</summary>
    Stored,

    /// <summary>The readings are stored and the message has field errors.</summary>
    StoredWithErrors,

    /// <summary>An envelope error: the message is stored with its error code and no readings.</summary>
    Rejected,

    /// <summary>The same machine and sampling time are stored already: nothing is stored.</summary>
    Duplicate,
}
