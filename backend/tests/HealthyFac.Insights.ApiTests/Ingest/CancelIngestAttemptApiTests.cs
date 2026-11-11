using System.Net;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.ApiTests.Ingest.Data;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Messaging.Ingest;

namespace HealthyFac.Insights.ApiTests.Ingest;

[Collection(InsightsApiCollection.Name)]
public sealed class CancelIngestAttemptApiTests(InsightsApiFixture fixture)
{
    private readonly InsightsApiFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(CancelIngestAttemptTestData.CancellableStates),
        MemberType = typeof(CancelIngestAttemptTestData)
    )]
    public async Task Cancel_ForActiveAttempt_InterruptsItAndQueuesTheMessage(IngestState state)
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(state);

        var response = await CancelAsync(attemptId);
        var attempts = await _fixture.LoadAttemptsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var attempt = attempts.Should().ContainSingle().Which;
        attempt.State.Should().Be(IngestState.Interrupted);
        attempt.InterruptReason.Should().Be(IngestInterruptReason.Cancelled);
        attempt.FinishedAt.Should().NotBeNull();

        messages.Should().ContainSingle().Which.State.Should().Be(IngestMessageStates.Interrupted);
    }

    [Theory]
    [MemberData(
        nameof(CancelIngestAttemptTestData.FinalStates),
        MemberType = typeof(CancelIngestAttemptTestData)
    )]
    public async Task Cancel_ForFinalAttempt_Returns409AndChangesNothing(IngestState state)
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(state);

        var response = await CancelAsync(attemptId);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var attempts = await _fixture.LoadAttemptsAsync();
        var messages = await _fixture.ReadOutboxMessagesAsync<IngestStateChanged>();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        content.Should().Contain(IngestErrors.IngestFinal);
        attempts.Should().ContainSingle().Which.State.Should().Be(state);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_ForAttemptOfAnotherCompany_Returns404()
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(IngestState.Queued, Guid.CreateVersion7());

        var response = await CancelAsync(attemptId);
        var attempts = await _fixture.LoadAttemptsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attempts.Should().ContainSingle().Which.State.Should().Be(IngestState.Queued);
    }

    [Fact]
    public async Task Cancel_WithoutWritePermission_Returns403()
    {
        var attemptId = await _fixture.ArrangeAttemptAsync(IngestState.Queued);

        var response = await _fixture.SendRequestAsync(
            HttpMethod.Post,
            CancelIngestAttemptTestData.CancelPath(_fixture.IdEncryptor.Encrypt(attemptId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.ReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> CancelAsync(Guid attemptId) =>
        _fixture.SendRequestAsync(
            HttpMethod.Post,
            CancelIngestAttemptTestData.CancelPath(_fixture.IdEncryptor.Encrypt(attemptId)),
            _fixture.CreateIngestClaims(StartIngestAttemptTestData.WritePermission));
}
