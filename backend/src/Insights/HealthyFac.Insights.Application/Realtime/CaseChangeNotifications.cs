using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Insights.Application.Realtime;

/// <summary>
/// Adds the real-time events of a case change. The methods only add events and read no data.
/// </summary>
public static class CaseChangeNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds the <c>cases</c> hint with the case. Call it after the save that changed the case returned.
        /// </summary>
        /// <param name="companyId">The company of the case.</param>
        /// <param name="caseId">The case that changed.</param>
        public void AddCasesHint(Guid companyId, Guid caseId) =>
            notifier.AddHint(companyId, new RealtimeHint { Topic = RealtimeTopic.Cases, CaseId = caseId });
    }
}
