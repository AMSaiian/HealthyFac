using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Lists how a user resolved a proposal. The camelCase values reach the message body, so never rename a value in
/// place: stored outbox and inbox rows use it.
/// </summary>
public sealed class DraftResolution : Enumeration<DraftResolution>
{
    public static readonly DraftResolution Accepted = new(value: "accepted");
    public static readonly DraftResolution Rejected = new(value: "rejected");

    private DraftResolution(string value)
        : base(value)
    {
    }
}
