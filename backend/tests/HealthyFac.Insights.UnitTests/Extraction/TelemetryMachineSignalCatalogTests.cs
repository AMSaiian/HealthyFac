using System.Net;
using System.Net.Http.Json;
using HealthyFac.Insights.Infrastructure.Extraction;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class TelemetryMachineSignalCatalogTests
{
    private const int PageSize = 100;
    private const int StatusCount = 250;

    private static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    private readonly DownstreamScopeHolder _holder = new();
    private readonly ScriptedHttpClientFactory _factory;
    private readonly TelemetryMachineSignalCatalog _catalog;

    public TelemetryMachineSignalCatalogTests()
    {
        _factory = new ScriptedHttpClientFactory(_holder);
        _catalog = new TelemetryMachineSignalCatalog(_factory, _holder, new PrefixIdEncryptor());
    }

    [Fact]
    public async Task Get_WithManyStatusDefinitions_ReadsEveryPageAndTheMetricKeys()
    {
        _factory.Respond = (request, body) => request.Method == HttpMethod.Get
            ? Json(new { definitions = new[] { new { key = "temp" }, new { key = "pressure" } } })
            : Json(StatusPage(body!));

        var signals = await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        signals.MetricKeys.Should().Equal("temp", "pressure");
        signals.StatusCodes.Should().HaveCount(StatusCount).And.OnlyHaveUniqueItems();

        var searches = _factory.Calls.Where(call => call.Method == HttpMethod.Post.Method).ToList();
        searches.Should().HaveCount(expected: 3);
        searches.Select(call => call.Body)
            .Should()
            .AllSatisfy(body => body.Should().Contain(expected: "\"limit\":100"));
        searches.Select(call => OffsetOf(call.Body!)).Should().Equal(0, 100, 200);
    }

    [Fact]
    public async Task Get_Always_EncryptsTheMachineIdInThePathAndEntersTheScopeForTheCallsOnly()
    {
        _factory.Respond = (request, body) => request.Method == HttpMethod.Get
            ? Json(new { definitions = Array.Empty<object>() })
            : Json(new { items = Array.Empty<object>(), totalCount = 0 });

        await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        var encrypted = $"{PrefixIdEncryptor.Prefix}{MachineId:N}";
        _factory.Calls.Should().OnlyContain(call => call.Path.Contains(encrypted));
        _factory.Calls.Should().OnlyContain(call => call.Scopes.SequenceEqual(new[] { "telemetry" }));
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WhenTelemetryHasNoMachine_ReturnsEmptyListsWithoutReadingStatuses()
    {
        _factory.Respond = (_, _) => new HttpResponseMessage(HttpStatusCode.NotFound);

        var signals = await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        signals.MetricKeys.Should().BeEmpty();
        signals.StatusCodes.Should().BeEmpty();
        _factory.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_WhenStatusSearchAnswersNotFound_ReturnsEmptyLists()
    {
        _factory.Respond = (request, _) => request.Method == HttpMethod.Get
            ? Json(new { definitions = new[] { new { key = "temp" } } })
            : new HttpResponseMessage(HttpStatusCode.NotFound);

        var signals = await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        signals.MetricKeys.Should().BeEmpty();
        signals.StatusCodes.Should().BeEmpty();
    }

    private static HttpResponseMessage Json<T>(T payload) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(payload) };

    private static int OffsetOf(string body) =>
        body.Contains(value: "\"offset\":200") ? 200 : body.Contains(value: "\"offset\":100") ? 100 : 0;

    private static object StatusPage(string body)
    {
        var offset = OffsetOf(body);
        var items = Enumerable.Range(offset, Math.Min(PageSize, StatusCount - offset))
            .Select(index => new { code = $"E{index}" })
            .ToArray();

        return new { items, totalCount = StatusCount };
    }
}
