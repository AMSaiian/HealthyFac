using Grpc.Core;
using Grpc.Net.ClientFactory;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>Creates gRPC clients over an invoker that is never called, and records the name of each client.</summary>
public sealed class RecordingGrpcClientFactory : GrpcClientFactory
{
    public List<string> RequestedNames { get; } = [];

    public override TClient CreateClient<TClient>(string name)
    {
        RequestedNames.Add(name);

        return (TClient)Activator.CreateInstance(typeof(TClient), new UnusedInvoker())!;
    }

    private sealed class UnusedInvoker : CallInvoker
    {
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request
        ) => throw new NotSupportedException();

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request
        ) => throw new NotSupportedException();

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request
        ) => throw new NotSupportedException();

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options
        ) => throw new NotSupportedException();

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options
        ) => throw new NotSupportedException();
    }
}
