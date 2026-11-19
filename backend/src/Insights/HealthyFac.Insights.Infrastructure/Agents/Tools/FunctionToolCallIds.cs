using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Infrastructure.Agents.Tools;

/// <summary>
/// Reads the ID of the running tool call from the function invocation of the chat client. A repeated call of the model
/// keeps its ID, so the write tools find their first action again.
/// </summary>
public sealed class FunctionToolCallIds : IToolCallIds
{
    public void EndLoop()
    {
        if (FunctionInvokingChatClient.CurrentContext is { } invocation)
        {
            invocation.Terminate = true;
        }
    }

    public string Current() =>
        FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? Guid.NewGuid().ToString(format: "N");
}
