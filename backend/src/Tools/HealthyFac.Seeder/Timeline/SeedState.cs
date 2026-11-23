namespace HealthyFac.Seeder.Timeline;

/// <summary>What the databases hold before the seed starts.</summary>
public enum SeedState
{
    /// <summary>No company exists: the seed has not run.</summary>
    Empty,

    /// <summary>The company exists and the last draft of the seed exists.</summary>
    Complete,

    /// <summary>The company exists, but the last draft does not: an earlier run stopped or failed.</summary>
    Incomplete,
}
