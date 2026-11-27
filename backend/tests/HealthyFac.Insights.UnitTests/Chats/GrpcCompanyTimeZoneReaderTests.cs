using Grpc.Core;
using HealthyFac.Core.Contracts.Companies;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Chats.Reads;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Insights.UnitTests.Files.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class GrpcCompanyTimeZoneReaderTests
{
    private readonly FakeDocumentCallInvoker _invoker = new(new DownstreamScopeHolder());
    private readonly GrpcCompanyTimeZoneReader _reader;

    public GrpcCompanyTimeZoneReaderTests()
    {
        _reader = new GrpcCompanyTimeZoneReader(new FakeChatClientFactory(_invoker));
    }

    [Fact]
    public async Task ReadTimeZoneIdAsync_WithAReply_ReturnsTheZoneOfTheCompany()
    {
        _invoker.Respond = _ => new CompanyReply { Name = "Kalyna Works", TimeZoneId = "Europe/Kyiv" };

        var zoneId = await _reader.ReadTimeZoneIdAsync(TestContext.Current.CancellationToken);

        zoneId.Should().Be(expected: "Europe/Kyiv");
    }

    [Fact]
    public async Task ReadTimeZoneIdAsync_WhenCoreIsDown_ThrowsAProviderUnavailableException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = async () => await _reader.ReadTimeZoneIdAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }
}
