using System.Net;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.UnitTests.WorkOrders;

public sealed class CoreWorkOrderCatalogTests
{
    private static readonly Guid MachineId = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");

    private Func<HttpRequestMessage, HttpResponseMessage> _respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(content: "[]"),
    };

    [Theory]
    [MemberData(nameof(CoreWorkOrderCatalogTestData.ServerErrors), MemberType = typeof(CoreWorkOrderCatalogTestData))]
    public async Task ListOpen_WhenCoreAnswersAServerError_ThrowsProviderUnavailable(HttpStatusCode status)
    {
        _respond = _ => new HttpResponseMessage(status);

        var act = () => ListAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task ListOpen_WhenCoreCannotBeReached_ThrowsProviderUnavailable()
    {
        _respond = _ => throw new HttpRequestException(message: "refused");

        var act = () => ListAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task ListOpen_WhenTheCallTimesOut_ThrowsProviderUnavailable()
    {
        _respond = _ => throw new TaskCanceledException(message: "timeout");

        var act = () => ListAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task ListOpen_WhenCoreAnswersAClientError_KeepsTheHttpFailure()
    {
        _respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var act = () => ListAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ListOpen_WhenTheCallerCancels_KeepsTheCancellation()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        _respond = _ => throw new TaskCanceledException(message: "cancelled");

        var act = () => ListAsync(source.Token);

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    private Task<IReadOnlyList<HealthyFac.Insights.Application.WorkOrders.OpenWorkOrderInfo>> ListAsync(
        CancellationToken cancellationToken
    ) =>
        new CoreWorkOrderCatalog(new FakeFactory(this), new FakeEncryptor())
            .ListOpenAsync(MachineId, limit: 10, cancellationToken);

    private sealed class FakeFactory(CoreWorkOrderCatalogTests owner) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new FakeHandler(owner)) { BaseAddress = new Uri(uriString: "http://core.test/") };
    }

    private sealed class FakeHandler(CoreWorkOrderCatalogTests owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(owner._respond(request));
    }

    private sealed class FakeEncryptor : IIdEncryptor
    {
        public string Encrypt(Guid id) => id.ToString();

        public Guid Decrypt(string encryptedId) => Guid.Parse(encryptedId);

        public string Migrate(string encryptedId) => encryptedId;
    }
}
