using Grpc.Core;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>
/// Plays Telemetry for the definitions client: an answer function per call, and a record of the downstream scopes at
/// the time of each call.
/// </summary>
public sealed class ScriptedDefinitionsClients(IDownstreamScopeHolder downstreamScopes) : ITelemetryDefinitionsClients
{
    private readonly ScriptedInvoker _invoker = new(downstreamScopes);

    public Func<object, object> Respond
    {
        get => _invoker.Respond;
        set => _invoker.Respond = value;
    }

    public IReadOnlyList<IReadOnlyList<string>> ScopesPerCall => _invoker.ScopesPerCall;

    public TelemetryDefinitions.TelemetryDefinitionsClient Create() => new(_invoker);

    private sealed class ScriptedInvoker(IDownstreamScopeHolder downstreamScopes) : CallInvoker
    {
        private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;
        private readonly List<IReadOnlyList<string>> _scopes = [];

        public Func<object, object> Respond { get; set; } =
            _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "none"));

        public IReadOnlyList<IReadOnlyList<string>> ScopesPerCall => _scopes;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request
        )
        {
            _scopes.Add([.. _downstreamScopes.Current]);

            try
            {
                var response = (TResponse)Respond(request);

                return new AsyncUnaryCall<TResponse>(
                    Task.FromResult(response),
                    Task.FromResult(new Metadata()),
                    () => new Status(StatusCode.OK, string.Empty),
                    () => [],
                    () => { });
            }
            catch (RpcException exception)
            {
                return new AsyncUnaryCall<TResponse>(
                    Task.FromException<TResponse>(exception),
                    Task.FromResult(new Metadata()),
                    () => exception.Status,
                    () => [],
                    () => { });
            }
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
