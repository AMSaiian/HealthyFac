using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Telemetry.Contracts.Signals;

namespace HealthyFac.Insights.UnitTests.Signals.Data;

public sealed class FakeTelemetryClientFactory(CallInvoker invoker) : GrpcClientFactory
{
    private readonly CallInvoker _invoker = invoker;

    public override TClient CreateClient<TClient>(string name) =>
        (TClient)(object)new TelemetrySignals.TelemetrySignalsClient(_invoker);
}
