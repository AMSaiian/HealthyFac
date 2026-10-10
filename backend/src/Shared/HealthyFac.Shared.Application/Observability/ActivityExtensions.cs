using System.Diagnostics;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Shared.Application.Observability;

/// <summary>
/// Provides extension methods for tagging an activity and recording its outcome.
/// Every method accepts a null activity, because <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns null while no listener records the source. Call sites therefore need no null checks.
/// </summary>
public static class ActivityExtensions
{
    extension(Activity? activity)
    {
        /// <summary>
        /// Adds a tag to the activity.
        /// </summary>
        /// <param name="key">The tag key, taken from <see cref="ObservabilityConstants.Tags"/>.</param>
        /// <param name="value">The tag value.</param>
        /// <returns>The same activity, so calls can chain.</returns>
        public Activity? WithTag(string key, object? value)
        {
            activity?.SetTag(key, value);

            return activity;
        }

        /// <summary>
        /// Marks the activity as a successful outcome.
        /// </summary>
        /// <returns>The same activity, so calls can chain.</returns>
        public Activity? MarkSuccess()
        {
            activity?.SetTag(ObservabilityConstants.Tags.Outcome, ObservabilityConstants.Outcomes.Success);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return activity;
        }

        /// <summary>
        /// Marks the activity with the outcome that matches the exception. An expected rejection, such as a
        /// validation failure or a missing resource, takes <see cref="MarkRejected"/>. Any other exception
        /// takes <see cref="MarkFailure"/>.
        /// </summary>
        /// <param name="exception">The exception that ended the traced work.</param>
        /// <returns>The same activity, so calls can chain.</returns>
        public Activity? MarkException(Exception exception)
            => IsRejection(exception)
                ? activity.MarkRejected()
                : activity.MarkFailure(exception);

        /// <summary>
        /// Marks the activity as an expected rejection. The status stays unset and no exception event is
        /// recorded, because the service handled the request correctly. Dashboards that count errors then
        /// ignore caller mistakes.
        /// </summary>
        /// <returns>The same activity, so calls can chain.</returns>
        public Activity? MarkRejected()
        {
            activity?.SetTag(ObservabilityConstants.Tags.Outcome, ObservabilityConstants.Outcomes.Rejected);

            return activity;
        }

        /// <summary>
        /// Marks the activity as a failed outcome and records the exception on it.
        /// </summary>
        /// <param name="exception">The exception that ended the traced work.</param>
        /// <returns>The same activity, so calls can chain.</returns>
        public Activity? MarkFailure(Exception exception)
        {
            activity?.SetTag(ObservabilityConstants.Tags.Outcome, ObservabilityConstants.Outcomes.Failure);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);

            return activity;
        }
    }

    private static bool IsRejection(Exception exception)
        => exception is IRejection;
}
