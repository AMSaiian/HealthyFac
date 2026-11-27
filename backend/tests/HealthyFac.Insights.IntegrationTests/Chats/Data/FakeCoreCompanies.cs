using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.Companies;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// Replaces the Core companies service in the integration tests. It counts the calls, so a test sees whether the cache
/// answered.
/// </summary>
public sealed class FakeCoreCompanies
{
    public const string ZoneId = "Europe/Kyiv";

    private int _callCount;

    public FakeCoreCompanies()
    {
        Factory = new CompaniesClientFactory(new CountingInvoker(this));
    }

    public GrpcClientFactory Factory { get; }

    public int CallCount => _callCount;

    public void Reset() => Interlocked.Exchange(ref _callCount, value: 0);

    private sealed class CompaniesClientFactory(CallInvoker invoker) : GrpcClientFactory
    {
        public override TClient CreateClient<TClient>(string name) =>
            (TClient)Activator.CreateInstance(typeof(TClient), invoker)!;
    }

    private sealed class CountingInvoker(FakeCoreCompanies owner) : CallInvoker
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request
        )
        {
            Interlocked.Increment(ref owner._callCount);
            var reply = (TResponse)(object)new CompanyReply { Name = "Kalyna Works", TimeZoneId = ZoneId };

            return new AsyncUnaryCall<TResponse>(
                Task.FromResult(reply),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
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
