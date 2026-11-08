using Xunit.v3;

namespace HealthyFac.Testing.Resetting;

/// <summary>
/// Marks a collection fixture that returns its resources and its base data to the start state before every test, so
/// no test depends on what an earlier test left. xUnit calls <see cref="ResetAsync"/> through
/// <see cref="INotifyTestLifecycleAsync"/> before each test of the collection, so no test class can forget the call.
/// </summary>
public interface IResetBeforeEachTest : INotifyTestLifecycleAsync
{
    /// <summary>
    /// Resets the resources of the fixture (database, broker queues, cache) and restores the base data that the
    /// fixture created at start. A failure fails the test that triggered it.
    /// </summary>
    ValueTask ResetAsync();

    ValueTask INotifyTestLifecycleAsync.OnTestStartingAsync(IXunitTest test) => ResetAsync();

    ValueTask INotifyTestLifecycleAsync.OnTestFinishedAsync(IXunitTest test) => ValueTask.CompletedTask;
}
