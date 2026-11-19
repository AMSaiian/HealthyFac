using System.Text.Json;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// The JSON options of the model boundary. Both options write and read every ID as an encrypted token, so the model
/// never sees a raw GUID and the stored log holds none.
/// </summary>
public interface IAgentJson
{
    /// <summary>
    /// Options for the tool arguments and the tool results.
    /// </summary>
    JsonSerializerOptions Tools { get; }

    /// <summary>
    /// Options for the case snapshot in the first message of a run.
    /// </summary>
    JsonSerializerOptions Snapshots { get; }
}
