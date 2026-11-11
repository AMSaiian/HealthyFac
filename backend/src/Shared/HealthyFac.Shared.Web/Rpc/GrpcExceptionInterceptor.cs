using Grpc.Core;
using Grpc.Core.Interceptors;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Turns an exception of a gRPC call into a status. The application exceptions keep their message, because they
/// are written for the caller. Any other exception gets a fixed text and no details, and the interceptor logs it
/// once at <c>Error</c>.
/// </summary>
public sealed class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception);
        }
    }

    private RpcException ToRpcException(Exception exception)
    {
        switch (exception)
        {
            case NotFoundException:
                return new RpcException(new Status(StatusCode.NotFound, exception.Message));
            case ForbiddenAccessException:
                return new RpcException(new Status(StatusCode.PermissionDenied, exception.Message));
            case AppValidationException:
                return new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
            case OperationCanceledException:
                return new RpcException(new Status(StatusCode.Cancelled, string.Empty));
        }

        _logger.LogError(exception, message: "A gRPC call failed with an unexpected exception");

        return new RpcException(new Status(StatusCode.Internal, GrpcConstants.UnexpectedErrorDetail));
    }
}
