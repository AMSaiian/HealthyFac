using HealthyFac.Shared.UnitTests.Proxy.Data;
using Microsoft.AspNetCore.TestHost;

namespace HealthyFac.Shared.UnitTests.Proxy;

public sealed class ForwardedPrefixTests
{
    private const string PublicPrefix = "/api/core";

    [Fact]
    public async Task ForwardedPrefix_FromTrustedAddress_SetsPathBase()
    {
        using var host = await ForwardedPrefixHostFactory.StartAsync(ForwardedPrefixHostFactory.TrustedClientAddress);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/machines");
        request.Headers.Add(ForwardedPrefixHostFactory.ForwardedPrefixHeader, PublicPrefix);

        using var response = await client.SendAsync(request);

        var pathBase = await response.Content.ReadAsStringAsync();

        pathBase.Should().Be(PublicPrefix);
    }

    [Fact]
    public async Task ForwardedPrefix_FromUntrustedAddress_IsIgnored()
    {
        using var host = await ForwardedPrefixHostFactory.StartAsync(
            ForwardedPrefixHostFactory.UntrustedClientAddress);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/machines");
        request.Headers.Add(ForwardedPrefixHostFactory.ForwardedPrefixHeader, "/evil");

        using var response = await client.SendAsync(request);

        var pathBase = await response.Content.ReadAsStringAsync();

        pathBase.Should().BeEmpty();
    }
}
