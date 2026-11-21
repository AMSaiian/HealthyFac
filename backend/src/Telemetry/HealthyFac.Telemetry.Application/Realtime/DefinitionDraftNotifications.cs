using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Telemetry.Application.Realtime;

/// <summary>
/// Adds the real-time hint of a definition draft change. The method only adds the hint and reads no data.
/// </summary>
public static class DefinitionDraftNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds the <c>definitionDrafts</c> hint. Call it after the save that changed the drafts returned.
        /// </summary>
        /// <param name="companyId">The company of the drafts.</param>
        /// <param name="attemptId">The ingest attempt of the change, or <see langword="null"/>.</param>
        /// <param name="machineId">The machine of the change, or <see langword="null"/>.</param>
        public void AddDefinitionDraftsHint(Guid companyId, Guid? attemptId, Guid? machineId) =>
            notifier.AddHint(
                companyId,
                new RealtimeHint
                {
                    Topic = RealtimeTopic.DefinitionDrafts,
                    AttemptId = attemptId,
                    MachineId = machineId,
                });
    }
}
