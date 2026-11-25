namespace HealthyFac.Insights.Application.Chats.Drafts;

/// <summary>
/// Defines the texts that a propose tool of the chat gives the model when it rejects a call.
/// </summary>
public static class ChatDraftErrors
{
    public const string MachineUpdateAfterDefinitions =
        "Propose the update of an existing machine before any definition of that machine in this turn.";
}
