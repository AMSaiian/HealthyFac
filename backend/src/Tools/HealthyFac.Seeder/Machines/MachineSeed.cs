using HealthyFac.Seeder.Moments;

namespace HealthyFac.Seeder.Machines;

public sealed class MachineSeed
{
    public required string Key { get; init; }

    /// <summary>The ID of the machine draft of set 1 that the apply turns into the machine.</summary>
    public required Guid DraftId { get; init; }

    /// <summary>The key of the person that applies the drafts.</summary>
    public required string AppliedBy { get; init; }

    /// <summary>The apply of the machine draft. Its definitions follow 30 minutes later.</summary>
    public required RelativeMoment AppliedAt { get; init; }

    public required RelativeMoment ExpectedGoLiveOn { get; init; }
}
