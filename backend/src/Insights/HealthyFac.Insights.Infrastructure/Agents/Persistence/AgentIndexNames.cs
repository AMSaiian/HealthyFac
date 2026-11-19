namespace HealthyFac.Insights.Infrastructure.Agents.Persistence;

/// <summary>
/// Defines the names of the indexes of the AI log tables that code reads, so a name never differs between the mapping
/// and a check.
/// </summary>
public static class AgentIndexNames
{
    public const string RunStateDueAt = "ix_agent_runs_state_due_at";
    public const string RunCase = "ix_agent_runs_case_id";
    public const string RunIngestAttempt = "ix_agent_runs_ingest_attempt_id";
    public const string ToolCallSequence = "ux_agent_tool_calls_run_id_sequence";
    public const string ActionCall = "ux_agent_actions_run_id_call_id";
}
