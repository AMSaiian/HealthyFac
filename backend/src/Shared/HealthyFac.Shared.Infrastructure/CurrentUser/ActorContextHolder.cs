using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.CurrentUser;

/// <summary>
/// Holds the actor of one DI scope. Register it as scoped. The entry point of the scope (a request, an inbox
/// message, or a job) enters the actor, and <see cref="ICurrentUserProvider"/> reads it. The holder lives in
/// Infrastructure, so an Application handler cannot replace the actor and forge the audit fields.
/// </summary>
public sealed class ActorContextHolder
{
    public ActorContext? Current { get; private set; }

    /// <summary>
    /// Sets the actor of the scope until the returned lease is disposed.
    /// </summary>
    /// <param name="actor">The actor of the scope.</param>
    /// <returns>A lease that clears the actor when it is disposed. A request or a job disposes it when its scope
    /// ends. A processor disposes it after each claimed message, because a transaction operation can run again
    /// in the same scope after a transient failure and then claim another message.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scope already has an actor, because a second actor would hide whose operation this is.
    /// </exception>
    public IDisposable Enter(ActorContext actor)
    {
        if (Current is not null)
        {
            throw CurrentUserErrors.ActorAlreadySet();
        }

        Current = actor;

        return new ActorLease(this);
    }

    private sealed class ActorLease(ActorContextHolder holder) : IDisposable
    {
        public void Dispose() => holder.Current = null;
    }
}
