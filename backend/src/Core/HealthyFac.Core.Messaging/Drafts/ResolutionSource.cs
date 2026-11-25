using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Lists where the proposal of a <see cref="DraftsResolved"/> message came from. The camelCase values reach the message
/// body, so never rename a value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class ResolutionSource : Enumeration<ResolutionSource>
{
    public static readonly ResolutionSource Ingest = new(value: "ingest");
    public static readonly ResolutionSource Chat = new(value: "chat");

    private ResolutionSource(string value)
        : base(value)
    {
    }
}
