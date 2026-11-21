namespace HealthyFac.Shared.Infrastructure.Messaging.Receiving;

/// <summary>
/// Observes the unit of work of the inbox runner. A feature that must act around one handled row registers an
/// observer (scoped), so the runner knows nothing about the feature. The runner calls the observers in order.
/// </summary>
public interface IInboxUnitObserver
{
    /// <summary>
    /// Runs first inside the transaction operation. The operation can run again after a transient failure, so the
    /// method resets any state of the previous attempt.
    /// </summary>
    void OnUnitStarting();

    /// <summary>
    /// Runs once after the transaction of a handled row committed. A failed or rolled-back attempt never calls it.
    /// </summary>
    void OnUnitCommitted();
}
