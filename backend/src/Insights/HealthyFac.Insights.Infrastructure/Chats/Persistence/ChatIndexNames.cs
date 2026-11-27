namespace HealthyFac.Insights.Infrastructure.Chats.Persistence;

/// <summary>
/// Defines the names of the indexes of the chat tables that code reads, so a name never differs between the mapping
/// and a check.
/// </summary>
public static class ChatIndexNames
{
    public const string OwnerList = "ix_chats_owner_user_id_company_id_last_activity_at";
    public const string MessageSequence = "ux_chat_messages_chat_id_sequence";
    public const string MessageRun = "ix_chat_messages_agent_run_id";
}
