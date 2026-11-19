using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.IdEncryption.Json;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Infrastructure.Agents.Tools;

/// <summary>
/// Builds the model boundary options with the shared ID encryption modifier.
/// </summary>
public sealed class AgentJson : IAgentJson
{
    public AgentJson(IIdEncryptor encryptor)
    {
        Tools = Encrypting(new JsonSerializerOptions(AIJsonUtilities.DefaultOptions), encryptor);
        Snapshots = Encrypting(new JsonSerializerOptions(JsonSerializerDefaults.Web), encryptor);
    }

    public JsonSerializerOptions Tools { get; }

    public JsonSerializerOptions Snapshots { get; }

    private static JsonSerializerOptions Encrypting(JsonSerializerOptions options, IIdEncryptor encryptor)
    {
        var resolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver =
            resolver.WithAddedModifier(typeInfo => IdEncryptionJsonModifier.Apply(typeInfo, encryptor));
        options.MakeReadOnly();

        return options;
    }
}
