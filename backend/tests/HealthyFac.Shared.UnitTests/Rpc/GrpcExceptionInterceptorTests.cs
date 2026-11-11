using Grpc.Core;
using HealthyFac.Shared.UnitTests.Rpc.Data;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyFac.Shared.UnitTests.Rpc;

public sealed class GrpcExceptionInterceptorTests
{
    private readonly GrpcExceptionInterceptor _interceptor = new(NullLogger<GrpcExceptionInterceptor>.Instance);

    [Theory]
    [MemberData(nameof(GrpcExceptionTestData.Mappings), MemberType = typeof(GrpcExceptionTestData))]
    public async Task UnaryServerHandler_WithException_MapsToStatus(Exception exception, StatusCode expected)
    {
        var act = () => _interceptor.UnaryServerHandler<string, string>(
            request: "request",
            null!,
            (_, _) => throw exception);

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task UnaryServerHandler_WithUnexpectedException_HidesDetails()
    {
        var act = () => _interceptor.UnaryServerHandler<string, string>(
            request: "request",
            null!,
            (_, _) => throw new InvalidOperationException(GrpcExceptionTestData.SecretDetail));

        var thrown = await act.Should().ThrowAsync<RpcException>();

        thrown.Which.Status.Detail.Should().NotContain(GrpcExceptionTestData.SecretDetail);
    }
}
