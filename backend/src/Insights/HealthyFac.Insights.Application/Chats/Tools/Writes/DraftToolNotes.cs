namespace HealthyFac.Insights.Application.Chats.Tools.Writes;

/// <summary>
/// The line that each chat propose tool adds to its result, so the model answers after a proposal instead of ending
/// the turn with no text.
/// </summary>
public static class DraftToolNotes
{
    public const string TellTheUser = "Tell the user which drafts you prepared.";
}
