namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// Gives access to the function invocation of the chat client that runs the tool call now: the ID that the model gave
/// the call, and the end of the function loop. Infrastructure reads both from the invocation.
/// </summary>
public interface IToolCallIds
{
    /// <summary>
    /// Returns the ID of the current call. Outside a call it returns a new ID.
    /// </summary>
    /// <returns>The call ID.</returns>
    string Current();

    /// <summary>
    /// Ends the function loop after the current call, so the model gets no further turn. Outside a call it does
    /// nothing.
    /// </summary>
    void EndLoop();
}
