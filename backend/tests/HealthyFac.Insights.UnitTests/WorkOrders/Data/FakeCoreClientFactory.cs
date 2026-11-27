using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.WorkOrders;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

public sealed class FakeCoreClientFactory(CallInvoker invoker) : GrpcClientFactory
{
    private readonly CallInvoker _invoker = invoker;

    public override TClient CreateClient<TClient>(string name) =>
        (TClient)(object)new CoreWorkOrders.CoreWorkOrdersClient(_invoker);
}
