using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Realtime;

namespace HealthyFac.Insights.Application.Realtime;

/// <summary>
/// Adds the real-time event of an ingest end. The method only adds the event and reads no data.
/// </summary>
public static class IngestChangeNotifications
{
    extension(IRealtimeNotifier notifier)
    {
        /// <summary>
        /// Adds <c>ingestFinished</c> for the user who started the attempt. Call it after the save that ended the
        /// attempt returned. The notifier skips the event when the actor is the user.
        /// </summary>
        /// <param name="createdBy">The user who started the attempt.</param>
        /// <param name="attemptId">The attempt that ended.</param>
        /// <param name="fileName">The name of the file of the attempt.</param>
        /// <param name="state">The final state: <c>completed</c>, <c>failed</c>, or <c>interrupted</c>.</param>
        /// <param name="isActorSkipped">
        /// <see langword="false"/> for a worker end, <see langword="true"/> for an action of the actor.
        /// </param>
        public void AddIngestFinished(
            Guid createdBy,
            Guid attemptId,
            string fileName,
            IngestState state,
            bool isActorSkipped
        ) =>
            notifier.AddToUser(
                createdBy,
                RealtimeNotificationKind.IngestFinished,
                new IngestFinishedData { AttemptId = attemptId, FileName = fileName, State = state },
                isActorSkipped);
    }
}
