using Grpc.Core;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.UnitTests.Files.Data;

/// <summary>
/// Answers Document gRPC calls from a delegate and records the downstream scopes at the time of each call.
/// </summary>
public sealed class FakeDocumentCallInvoker(IDownstreamScopeHolder downstreamScopes) : CallInvoker
{
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public Func<object, object> Respond { get; set; } = _ => throw new InvalidOperationException();

    public List<IReadOnlyCollection<string>> ScopesAtCall { get; } = [];

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request
    )
    {
        ScopesAtCall.Add(_downstreamScopes.Current.ToList());

        Task<TResponse> response;
        try
        {
            response = Task.FromResult((TResponse)Respond(request));
        }
        catch (RpcException exception)
        {
            response = Task.FromException<TResponse>(exception);
        }

        return new AsyncUnaryCall<TResponse>(
            response,
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
