using System.Net;
using System.Text.RegularExpressions;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class DocumentBlobRouteTests(GatewayEnvironment environment, AzuriteBlobFixture storage)
    : IClassFixture<AzuriteBlobFixture>, IAsyncLifetime
{
    private const string LevelsSection = "Serilog:MinimumLevel";
    private const string SignaturePattern = "sig=([^&]+)";
    private const string NoSniff = "nosniff";
    private const string ContentTypeOptions = "X-Content-Type-Options";

    private readonly GatewayEnvironment _environment = environment;
    private readonly AzuriteBlobFixture _storage = storage;
    private readonly EchoServer _core = new();

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task BlobRoute_WithValidLink_ReturnsTheBytesWithTheSignedHeadersAndNoSniff()
    {
        var id = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var query = _storage.SignRead(
            id,
            DateTimeOffset.UtcNow.AddMinutes(minutes: 5),
            DocumentBlobRouteTestData.PdfType,
            DocumentBlobRouteTestData.AttachmentDisposition);
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(Path(id, query), TestContext.Current.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        bytes.Should().Equal(DocumentBlobRouteTestData.Content);
        response.Content.Headers.ContentType!.MediaType.Should().Be(DocumentBlobRouteTestData.PdfType);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be(expected: "attachment");
        response.Headers.GetValues(ContentTypeOptions).Should().Equal(NoSniff);
    }

    [Fact]
    public async Task BlobRoute_WithExpiredLink_Returns403()
    {
        var id = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var query = _storage.SignRead(id, DateTimeOffset.UtcNow.AddMinutes(minutes: -1));
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(Path(id, query), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlobRoute_WithChangedSignature_Returns403()
    {
        var id = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var query = _storage.SignRead(id, DateTimeOffset.UtcNow.AddMinutes(minutes: 5))
            .Replace(DocumentBlobRouteTestData.SignatureMarker, $"{DocumentBlobRouteTestData.SignatureMarker}A");
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(Path(id, query), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlobRoute_WithLinkOfAnotherBlob_Returns403()
    {
        var first = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var second = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var queryOfFirst = _storage.SignRead(first, DateTimeOffset.UtcNow.AddMinutes(minutes: 5));
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(Path(second, queryOfFirst), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlobRoute_WithBearerTokenAndCookie_DoesNotForwardThemToTheStorage()
    {
        await using var storageStandIn = new EchoServer();
        await storageStandIn.StartAsync();
        await using var gateway = _environment.CreateGateway(
            _core,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.DocumentStorageAddress] = storageStandIn.Address,
            });
        using var client = gateway.CreateAuthenticatedClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, Path(Guid.CreateVersion7(), query: "sp=r"));
        request.Headers.Add(DocumentBlobRouteTestData.CookieHeader, DocumentBlobRouteTestData.CookieValue);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var forwardedRequest = storageStandIn.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Headers.Should().NotContainKey(DocumentBlobRouteTestData.AuthorizationHeader);
        forwardedRequest.Headers.Should().NotContainKey(DocumentBlobRouteTestData.CookieHeader);
    }

    [Theory]
    [MemberData(nameof(DocumentBlobRouteTestData.WriteMethods), MemberType = typeof(DocumentBlobRouteTestData))]
    public async Task BlobRoute_WithWriteMethod_NeverChangesTheBlob(string method, HttpStatusCode expectedStatus)
    {
        var id = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var query = _storage.SignRead(id, DateTimeOffset.UtcNow.AddMinutes(minutes: 5));
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), Path(id, query))
        {
            Content = new StringContent(DocumentBlobRouteTestData.StaleUpdateBody),
        };

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var stored = await _storage.ReadAsync(id);

        response.StatusCode.Should().Be(expectedStatus);
        stored.Should().Equal(DocumentBlobRouteTestData.Content);
    }

    [Fact]
    public async Task BlobRoute_WithLink_LogsAndTracesNoSignature()
    {
        var id = await _storage.UploadAsync(DocumentBlobRouteTestData.Content, DocumentBlobRouteTestData.PdfType);
        var query = _storage.SignRead(id, DateTimeOffset.UtcNow.AddMinutes(minutes: 5));
        var encodedSignature = Regex.Match(query, SignaturePattern).Groups[groupnum: 1].Value;
        var signatures = new[] { encodedSignature, Uri.UnescapeDataString(encodedSignature) };
        var spans = new SpanTagCapture();
        var logs = new LogTextCapture();
        await using var gateway = CreateGateway(
            new Dictionary<string, string>
            {
                [DocumentBlobRouteTestData.TracingEnabledKey] = DocumentBlobRouteTestData.TracingEnabledValue,
            },
            services => services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(spans)));
        _ = gateway.Services;
        Log.Logger = CreateLogger(gateway.Services.GetRequiredService<IConfiguration>(), logs);
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync(Path(id, query), TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(seconds: 1), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        logs.Lines.Should().Contain(line => line.Contains("responded") && line.Contains(id.ToString()));
        logs.Lines.Should().NotContain(line => signatures.Any(line.Contains));
        spans.TagValues.Should().Contain(tag => tag.Contains(id.ToString()));
        spans.TagValues.Should().NotContain(tag => signatures.Any(tag.Contains));
    }

    /// <summary>
    /// Builds a logger that applies the minimum levels of the gateway configuration and writes to the capture only, so
    /// the test sees the log entries that the configured levels let through.
    /// </summary>
    private static Logger CreateLogger(IConfiguration gatewayConfiguration, LogTextCapture logs)
    {
        var levels = new ConfigurationBuilder()
            .AddInMemoryCollection(
                gatewayConfiguration.GetSection(LevelsSection)
                    .AsEnumerable(makePathsRelative: true)
                    .Where(pair => pair.Value is not null)
                    .Select(pair => new KeyValuePair<string, string?>($"{LevelsSection}:{pair.Key}", pair.Value)))
            .Build();

        return new LoggerConfiguration().ReadFrom.Configuration(levels).WriteTo.Sink(logs).CreateLogger();
    }

    private static string Path(Guid id, string query) =>
        string.Format(DocumentBlobRouteTestData.BlobPathFormat, id, query);

    private GatewayFactory CreateGateway(
        IReadOnlyDictionary<string, string>? extraSettings = null,
        Action<IServiceCollection>? configureServices = null
    )
    {
        var settings = new Dictionary<string, string>
        {
            [GatewaySettingKeys.DocumentStorageAddress] = _storage.Address.AbsoluteUri,
        };

        foreach (var (key, value) in extraSettings ?? new Dictionary<string, string>())
        {
            settings[key] = value;
        }

        return _environment.CreateGateway(_core, settings: settings, configureServices: configureServices);
    }
}
