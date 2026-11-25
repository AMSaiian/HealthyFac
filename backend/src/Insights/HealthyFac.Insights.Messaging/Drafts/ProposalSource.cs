using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Lists where the proposal of a <see cref="DraftsProposed"/> message came from. The camelCase values reach the message
/// body, so never rename a value in place: stored outbox and inbox rows use it.
/// </summary>
public sealed class ProposalSource : Enumeration<ProposalSource>
{
    public static readonly ProposalSource Ingest = new(value: "ingest");
    public static readonly ProposalSource Chat = new(value: "chat");

    private ProposalSource(string value)
        : base(value)
    {
    }
}
