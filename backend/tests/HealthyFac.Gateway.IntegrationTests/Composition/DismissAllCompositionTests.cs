using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Testing.Features;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Drafts)]
public sealed class DismissAllCompositionTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string CountsCoreTwo = """{"dismissedCount":2}""";
    private const string CountsTelemetryThree = """{"dismissedCount":3}""";

    private readonly GatewayEnvironment _environment = environment;
    private readonly CompositionStubs _stubs = new();

    public async ValueTask InitializeAsync() => await _stubs.StartAsync();

    public async ValueTask DisposeAsync() => await _stubs.DisposeAsync();

    [Fact]
    public async Task DismissMachineDrafts_WithBothServices_ReturnsTheSumOfTheCounts()
    {
        _stubs.Core.Respond(HttpMethod.Post, CompositionStubs.CoreMachineDismiss, status: 200, CountsCoreTwo);

        _stubs.Telemetry.Respond(
            HttpMethod.Post, CompositionStubs.TelemetryMachineDismiss, status: 200, CountsTelemetryThree);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            CompositionStubs.PublicMachineDismiss, content: null, TestContext.Current.CancellationToken);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["dismissedCount"]!.GetValue<int>().Should().Be(expected: 5);
    }

    [Fact]
    public async Task DismissAttemptDrafts_WithBothServices_ReturnsTheSumOfTheCounts()
    {
        _stubs.Core.Respond(HttpMethod.Post, CompositionStubs.CoreAttemptDismiss, status: 200, CountsCoreTwo);

        _stubs.Telemetry.Respond(
            HttpMethod.Post, CompositionStubs.TelemetryAttemptDismiss, status: 200, CountsTelemetryThree);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            CompositionStubs.PublicAttemptDismiss, content: null, TestContext.Current.CancellationToken);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        body["dismissedCount"]!.GetValue<int>().Should().Be(expected: 5);
    }

    [Fact]
    public async Task DismissChatDrafts_WithBothServices_CallsTheChatPathsAndReturnsTheSumOfTheCounts()
    {
        _stubs.Core.Respond(HttpMethod.Post, CompositionStubs.CoreChatDismiss, status: 200, CountsCoreTwo);

        _stubs.Telemetry.Respond(
            HttpMethod.Post, CompositionStubs.TelemetryChatDismiss, status: 200, CountsTelemetryThree);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            CompositionStubs.PublicChatDismiss, content: null, TestContext.Current.CancellationToken);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        body["dismissedCount"]!.GetValue<int>().Should().Be(expected: 5);
        _stubs.Core.Requests.Select(request => request.Path).Should().Equal(CompositionStubs.CoreChatDismiss);

        _stubs.Telemetry.Requests.Select(request => request.Path)
            .Should()
            .Equal(CompositionStubs.TelemetryChatDismiss);
    }

    [Fact]
    public async Task DismissMachineDrafts_WhenCoreAnswers404_Returns404()
    {
        _stubs.Core.Respond(
            HttpMethod.Post,
            CompositionStubs.CoreMachineDismiss,
            status: 404,
            CompositionFailureTestData.NotFoundJson);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.PostAsync(
            CompositionStubs.PublicMachineDismiss, content: null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
