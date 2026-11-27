namespace HealthyFac.Core.Infrastructure.Drafts.Persistence;

/// <summary>
/// Defines the index names of the draft tables.
/// </summary>
public static class DraftIndexNames
{
    public const string MachineDraftProposal = "ix_machine_drafts_company_id_proposal_id";
    public const string ProposalAttempt = "ix_draft_proposals_company_id_attempt_id";
    public const string ProposalChat = "ix_draft_proposals_company_id_chat_id";
    public const string ProposalRun = "ix_draft_proposals_run_id";
    public const string ProposalChatMessage = "ix_draft_proposals_chat_message_id";
    public const string MachineDraftTarget = "ix_machine_drafts_target_machine_id_state";
}
