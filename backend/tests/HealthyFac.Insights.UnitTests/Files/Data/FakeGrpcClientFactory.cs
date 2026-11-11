using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Document.Contracts.Files;

namespace HealthyFac.Insights.UnitTests.Files.Data;

public sealed class FakeGrpcClientFactory(CallInvoker invoker) : GrpcClientFactory
{
    private readonly CallInvoker _invoker = invoker;

    public override TClient CreateClient<TClient>(string name) =>
        (TClient)(object)new DocumentFiles.DocumentFilesClient(_invoker);
}
