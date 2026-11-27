using Grpc.Core;
using Grpc.Net.ClientFactory;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

/// <summary>
/// Creates every gRPC client over one call invoker, whatever its name, so a test plays Core, Telemetry, and Document
/// with one delegate.
/// </summary>
public sealed class FakeChatClientFactory(CallInvoker invoker) : GrpcClientFactory
{
    private readonly CallInvoker _invoker = invoker;

    public override TClient CreateClient<TClient>(string name) =>
        (TClient)Activator.CreateInstance(typeof(TClient), _invoker)!;
}
