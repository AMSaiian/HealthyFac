using System.Net;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.ApiTests.Ingest.Data;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.ApiTests.Ingest;

[Collection(InsightsApiCollection.Name)]
public sealed class StartIngestAttemptApiTests(InsightsApiFixture fixture)
{
    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task Start_ForPdfAttachment_StoresTheQueuedAttemptWithTheSnapshotAndTheMessage()
    {
        var machineId = Guid.CreateVersion7();
        _fixture.ArrangeFile(
            StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Attachment, machineId: machineId);

        var response = await StartAsync();
        var attempts = await _fixture.LoadAttemptsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var attempt = attempts.Should().ContainSingle().Which;
        attempt.State.Should().Be(IngestState.Queued);
        attempt.FileId.Should().Be(StartIngestAttemptTestData.FileId);
        attempt.CompanyId.Should().Be(_fixture.CompanyId);
        attempt.MachineId.Should().Be(machineId);
        attempt.MachineChangedAt.Should().Be(StartIngestAttemptTestData.ChangedAt);
        attempt.FileName.Should().Be("manual");
        attempt.Extension.Should().Be(StartIngestAttemptTestData.Pdf);
        attempt.SizeBytes.Should().Be(expected: 2048);

        var message = messages.Should().ContainSingle().Which;
        message.FileId.Should().Be(StartIngestAttemptTestData.FileId);
        message.State.Should().Be(IngestMessageStates.Queued);
        message.FailedCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Start_ForNotIngestableExtension_Returns400AndStoresNothing()
    {
        _fixture.ArrangeFile(StartIngestAttemptTestData.Png, StartIngestAttemptTestData.Attachment);

        var response = await StartAsync();
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var attempts = await _fixture.LoadAttemptsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain(IngestErrors.FileTypeNotIngestable);
        attempts.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_ForUnknownOrDeletedFile_Returns404()
    {
        var response = await StartAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_ForFileOfAnotherCompany_Returns404()
    {
        _fixture.ArrangeFile(
            StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Attachment, companyId: Guid.CreateVersion7());

        var response = await StartAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_ForEmbeddedImage_Returns404()
    {
        _fixture.ArrangeFile(StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Embedded);

        var response = await StartAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(
        nameof(StartIngestAttemptTestData.RefusedHistories),
        MemberType = typeof(StartIngestAttemptTestData)
    )]
    public async Task Start_WhenHistoryRefuses_Returns409WithTheKey(IngestState[] history, string key)
    {
        _fixture.ArrangeFile(StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Attachment);
        await _fixture.ArrangeAttemptsAsync(history);

        var response = await StartAsync();
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var attempts = await _fixture.LoadAttemptsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain(key);
        attempts.Should().HaveCount(history.Length);
    }

    [Theory]
    [MemberData(
        nameof(StartIngestAttemptTestData.AllowedHistories),
        MemberType = typeof(StartIngestAttemptTestData)
    )]
    public async Task Start_WhenHistoryAllows_Returns201AndCountsTheFailedAttempts(IngestState[] history)
    {
        _fixture.ArrangeFile(StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Attachment);
        await _fixture.ArrangeAttemptsAsync(history);

        var response = await StartAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();
        var failedCount = history.Count(state => state == IngestState.Failed);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        messages.Should().ContainSingle().Which.FailedCount.Should().Be(failedCount);
    }

    [Fact]
    public async Task Start_WithoutWritePermission_Returns403()
    {
        _fixture.ArrangeFile(StartIngestAttemptTestData.Pdf, StartIngestAttemptTestData.Attachment);

        var response = await _fixture.SendRequestAsync(
            HttpMethod.Post,
            StartIngestAttemptTestData.StartPath(_fixture.IdEncryptor.Encrypt(StartIngestAttemptTestData.FileId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.ReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Start_WithoutToken_Returns401()
    {
        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            StartIngestAttemptTestData.StartPath(_fixture.IdEncryptor.Encrypt(StartIngestAttemptTestData.FileId)));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private Task<HttpResponseMessage> StartAsync() =>
        _fixture.SendRequestAsync(
            HttpMethod.Post,
            StartIngestAttemptTestData.StartPath(_fixture.IdEncryptor.Encrypt(StartIngestAttemptTestData.FileId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.WritePermission));
}
