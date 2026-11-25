namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Holds what the tools of one chat turn tell the turn runner. One instance serves the request scope of the turn, so
/// the runner and the handler of a tool see the same values.
/// </summary>
public sealed class ChatTurnState
{
    /// <summary>
    /// Tells that the model called <c>report_no_answer</c>: neither the data of the company nor general knowledge
    /// answers the question.
    /// </summary>
    public bool IsNoAnswerReported { get; set; }
}
