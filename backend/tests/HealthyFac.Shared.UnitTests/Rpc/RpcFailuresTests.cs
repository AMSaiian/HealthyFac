using Grpc.Core;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.UnitTests.Rpc.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Rpc;

[Feature(TestFeatures.Platform)]
public sealed class RpcFailuresTests
{
    [Theory]
    [MemberData(nameof(RpcFailuresTestData.Statuses), MemberType = typeof(RpcFailuresTestData))]
    public void IsOutage_ByStatus_IsTrueOnlyForUnavailableAndDeadlineExceeded(
        string caseName,
        StatusCode code,
        bool expected
    )
    {
        var isOutage = RpcFailures.IsOutage(new RpcException(new Status(code, detail: "failure")));

        isOutage.Should().Be(expected, caseName);
    }

    [Theory]
    [MemberData(nameof(RpcFailuresTestData.Cancellations), MemberType = typeof(RpcFailuresTestData))]
    public void IsCancelledByCaller_ByTokenAndStatus_IsTrueOnlyForACancelledCallOfACancelledCaller(
        string caseName,
        CancellationToken token,
        StatusCode code,
        bool expected
    )
    {
        var isCancelled = RpcFailures.IsCancelledByCaller(
            new RpcException(new Status(code, detail: "failure")), token);

        isCancelled.Should().Be(expected, caseName);
    }
}
