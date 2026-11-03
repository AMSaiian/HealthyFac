using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

namespace HealthyFac.Spa.IntegrationTests.Hosting;

public sealed partial class SpaHostTests
{
    [Fact]
    public async Task GetRoot_Always_ReturnsIndexWithSameNonceInHtmlAndHeader()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        var htmlNonces = NonceInHtml().Matches(html).Select(match => match.Groups[1].Value).ToList();
        var headerNonce = NonceInHeader()
            .Match(response.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Single())
            .Groups[1].Value;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain(SpaFactory.NoncePlaceholder);
        htmlNonces.Should().HaveCount(2);
        htmlNonces.Should().OnlyContain(nonce => nonce == headerNonce && nonce.Length > 0);
    }

    [Fact]
    public async Task GetRoot_TwoRequests_UseDifferentNonces()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/");

        var firstHeader = first.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Single();
        var secondHeader = second.Headers.GetValues(HeaderNames.ContentSecurityPolicy).Single();

        NonceInHeader()
            .Match(firstHeader)
            .Groups[1]
            .Value.Should()
            .NotBe(NonceInHeader().Match(secondHeader).Groups[1].Value);
    }

    [Fact]
    public async Task GetUnknownRouteWithoutExtension_ReturnsIndex()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/machines/42");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.ToString().Should().Be("text/html; charset=utf-8");
        html.Should().Contain("hf-root");
        response.Headers.GetValues(HeaderNames.CacheControl).Should().Contain("no-cache");
        response.Headers.GetValues(HeaderNames.XContentTypeOptions).Should().Contain("nosniff");
    }

    [Fact]
    public async Task GetMissingFileWithExtension_Returns404()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/missing-12345678.js");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHashedAsset_ReturnsImmutableCache()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync($"/{SpaFactory.HashedAssetName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().Should().Be("public, max-age=31536000, immutable");
    }

    [Fact]
    public async Task GetHashedAsset_WithConfiguredSeconds_ReturnsThatMaxAge()
    {
        await using var spa = new SpaFactory(
            settings: new Dictionary<string, string> { ["Spa:ImmutableCacheSeconds"] = "60" });
        using var client = spa.CreateClient();

        var response = await client.GetAsync($"/{SpaFactory.HashedAssetName}");

        response.Headers.CacheControl?.ToString().Should().Be("public, max-age=60, immutable");
    }

    [Fact]
    public async Task GetRoot_WithConfiguredPermissionsPolicy_ReturnsThatHeader()
    {
        await using var spa = new SpaFactory(
            settings: new Dictionary<string, string> { ["Spa:PermissionsPolicy"] = "camera=(self)" });
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/");

        response.Headers.GetValues("Permissions-Policy").Should().Equal("camera=(self)");
    }

    [Fact]
    public async Task GetFileWithHashLikeNameButNoDot_ReturnsNoCache()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync($"/{SpaFactory.LookalikeFileName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task GetUnhashedFile_ReturnsNoCache()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync($"/{SpaFactory.UnhashedFileName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task GetIndexHtmlPath_ReturnsIndexWithNonce()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/index.html");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain(SpaFactory.NoncePlaceholder);
        response.Headers.Contains(HeaderNames.ContentSecurityPolicy).Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigJson_ReturnsApiBaseUrlWithNoStore()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/config.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().Should().Be("no-store");
        json.RootElement.GetProperty("apiBaseUrl").GetString().Should().Be(SpaFactory.ApiBaseUrl);
    }

    [Fact]
    public async Task GetConfigJson_Always_ReturnsOnlyAllowListedProperties()
    {
        await using var spa = new SpaFactory();
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/config.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        json.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("apiBaseUrl");
    }

    [Fact]
    public async Task Startup_WithTelemetryEnabled_ServesIndex()
    {
        await using var spa = new SpaFactory(isTelemetryEnabled: true);
        using var client = spa.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Startup_WithMissingIndexFile_Fails()
    {
        using var spa = new SpaFactory(false);

        var act = () => spa.CreateClient();

        act.Should().Throw<FileNotFoundException>();
    }

    [GeneratedRegex("nonce=\"([^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex NonceInHtml();

    [GeneratedRegex("'nonce-([^']*)'")]
    private static partial Regex NonceInHeader();
}
