namespace HealthyFac.Core.IntegrationTests.Concurrency.Data;

public static class ConcurrentSaveGateTestData
{
    public const string FastParticipantSaved = "fast participant saved";
    public const string SlowParticipantReachedSave = "slow participant reached its save";
    public const string OneOfTwoArrived = "1 of 2";
    public const int ParticipantCount = 2;

    public static readonly TimeSpan SlowParticipantDelay = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan OutsideSaveLimit = TimeSpan.FromSeconds(5);
}
