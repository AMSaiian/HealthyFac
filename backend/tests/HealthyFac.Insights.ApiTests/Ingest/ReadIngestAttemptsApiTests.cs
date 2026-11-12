using System.Net;
using System.Text.Json;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.ApiTests.Ingest.Data;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;

namespace HealthyFac.Insights.ApiTests.Ingest;

[Collection(InsightsApiCollection.Name)]
public sealed class ReadIngestAttemptsApiTests(InsightsApiFixture fixture)
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";

    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task GetFileAttempts_WithSeveralAttempts_ReturnsTheNewestFirst()
    {
        var oldest = await _fixture.ArrangeAttemptAsync(
            IngestState.Interrupted, queuedAt: StartIngestAttemptTestData.ChangedAt);
        var newest = await _fixture.ArrangeAttemptAsync(
            IngestState.Interrupted, queuedAt: StartIngestAttemptTestData.ChangedAt.AddHours(value: 2));
        var middle = await _fixture.ArrangeAttemptAsync(
            IngestState.Interrupted, queuedAt: StartIngestAttemptTestData.ChangedAt.AddHours(value: 1));

        var response = await GetFileAttemptsAsync();
        var ids = await ReadItemIdsAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ids.Should().Equal(newest, middle, oldest);
    }

    [Fact]
    public async Task GetFileAttempts_WithMoreThanTheCap_ReturnsTheCapNewest()
    {
        await _fixture.ArrangeAttemptsAsync(
            [.. Enumerable.Repeat(IngestState.Interrupted, IngestRules.MaxListedAttempts + 1)]);

        var response = await GetFileAttemptsAsync();
        var ids = await ReadItemIdsAsync(response);

        ids.Should().HaveCount(IngestRules.MaxListedAttempts);
    }

    [Fact]
    public async Task GetFileAttempts_WithAttemptsOfAnotherCompany_ReturnsNoneOfThem()
    {
        await _fixture.ArrangeAttemptAsync(IngestState.Queued, Guid.CreateVersion7());

        var response = await GetFileAttemptsAsync();
        var ids = await ReadItemIdsAsync(response);

        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAttempt_ForOwnAttempt_ReturnsItsState()
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(IngestState.Failed);

        var response = await _fixture.SendRequestAsync(
            HttpMethod.Get,
            ReadIngestAttemptsTestData.AttemptPath(_fixture.IdEncryptor.Encrypt(attemptId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.ReadPermission));
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty(propertyName: "state").GetString().Should().Be(IngestState.Failed.Value);
        document.RootElement.GetProperty(propertyName: "errorCode")
            .GetString()
            .Should()
            .Be(IngestErrorCode.ProviderUnavailable.Value);
    }

    [Fact]
    public async Task GetAttempt_ForAttemptOfAnotherCompany_Returns404()
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(IngestState.Queued, Guid.CreateVersion7());

        var response = await _fixture.SendRequestAsync(
            HttpMethod.Get,
            ReadIngestAttemptsTestData.AttemptPath(_fixture.IdEncryptor.Encrypt(attemptId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.ReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(
        nameof(ReadIngestAttemptsTestData.RoutesWithoutReadPermission),
        MemberType = typeof(ReadIngestAttemptsTestData)
    )]
    public async Task Read_WithoutReadPermission_Returns403(string caseName, string pathTemplate)
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(IngestState.Queued);
        var path = pathTemplate
            .Replace("{fileId}", _fixture.IdEncryptor.Encrypt(StartIngestAttemptTestData.FileId))
            .Replace("{attemptId}", _fixture.IdEncryptor.Encrypt(attemptId));

        var response = await _fixture.SendRequestAsync(
            HttpMethod.Get,
            path,
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.WritePermission));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: caseName);
    }

    private Task<HttpResponseMessage> GetFileAttemptsAsync() =>
        _fixture.SendRequestAsync(
            HttpMethod.Get,
            StartIngestAttemptTestData.StartPath(_fixture.IdEncryptor.Encrypt(StartIngestAttemptTestData.FileId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.ReadPermission));

    private async Task<List<Guid>> ReadItemIdsAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        return document.RootElement.GetProperty(ItemsProperty)
            .EnumerateArray()
            .Select(item => _fixture.IdEncryptor.Decrypt(item.GetProperty(IdProperty).GetString()!))
            .ToList();
    }
}
