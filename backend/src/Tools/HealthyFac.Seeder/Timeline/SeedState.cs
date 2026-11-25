namespace HealthyFac.Seeder.Timeline;

/// <summary>What the databases hold before the seed starts.</summary>
public enum SeedState
{
    /// <summary>No company exists: the seed has not run.</summary>
    Empty,

    /// <summary>
    /// The company exists, the last draft of the seed exists, and the message that marks the telemetry history does
    /// not. The live machines write received messages, so other received messages tell nothing.
    /// </summary>
    Complete,

    /// <summary>
    /// The last draft exists and the Telemetry database holds the received message that marks the telemetry history.
    /// </summary>
    CompleteWithHistory,

    /// <summary>
    /// The company exists, but the last draft does not: an earlier run stopped or failed.
    /// </summary>
    Incomplete,
}
