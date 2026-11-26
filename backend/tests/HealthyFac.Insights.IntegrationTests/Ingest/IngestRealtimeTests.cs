using HealthyFac.Document.Messaging.Files;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Realtime;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Ingest)]
public sealed class IngestRealtimeTests(InsightsFixture fixture)
{
    private const string FileName = "manual";

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task RunOnce_WhenTheAttemptCompletes_RecordsIngestFinishedCompletedForTheCreator()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.RealtimeQueue.Clear();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        AssertOneEvent(attemptId, IngestState.Completed);
    }

    [Fact]
    public async Task RunOnce_WhenTheAttemptFails_RecordsIngestFinishedFailedForTheCreator()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.Files.Clear();
        _fixture.RealtimeQueue.Clear();

        await _fixture.RunWorkerAsync(TestContext.Current.CancellationToken);

        AssertOneEvent(attemptId, IngestState.Failed);
    }

    [Fact]
    public async Task RunOnce_WhenTheServiceStops_RecordsIngestFinishedInterruptedForTheCreator()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        _fixture.Ingest.Converter.Behavior = async cancellationToken =>
        {
            await stop.CancelAsync();
            await Task.Delay(Timeout.Infinite, cancellationToken);

            return FakeDocumentConverter.DefaultDocument;
        };

        _fixture.RealtimeQueue.Clear();

        var act = () => _fixture.RunWorkerAsync(stop.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        AssertOneEvent(attemptId, IngestState.Interrupted);
    }

    [Fact]
    public async Task CancelAttempt_ByTheCreator_RecordsNothing()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.RealtimeQueue.Clear();

        await _fixture.CancelAttemptAsync(attemptId);

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAttempt_ByAnotherUser_RecordsIngestFinishedInterruptedForTheCreator()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.RealtimeQueue.Clear();

        await _fixture.CancelAttemptAsync(attemptId, Guid.CreateVersion7());

        AssertOneEvent(attemptId, IngestState.Interrupted);
    }

    [Fact]
    [Feature(TestFeatures.Files)]
    public async Task FileDeleted_WithAnActiveAttempt_RecordsIngestFinishedInterruptedForTheCreator()
    {
        _fixture.ArrangeFile();
        var attemptId = await _fixture.StartAttemptAsync();
        _fixture.RealtimeQueue.Clear();

        await DeleteFileAndPushAsync();

        AssertOneEvent(attemptId, IngestState.Interrupted);
    }

    [Fact]
    [Feature(TestFeatures.Files)]
    public async Task FileDeleted_WithoutAnAttempt_RecordsNothing()
    {
        _fixture.RealtimeQueue.Clear();

        await DeleteFileAndPushAsync();

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }

    private void AssertOneEvent(Guid attemptId, IngestState state)
    {
        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.UserId.Should().Be(IngestWorkerTestData.UserId);
        push.Notification!.Kind.Should().Be(RealtimeNotificationKind.IngestFinished);

        push.Notification.Data.Should()
            .BeEquivalentTo(new IngestFinishedData { AttemptId = attemptId, FileName = FileName, State = state });
    }

    private async Task DeleteFileAndPushAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<FileDeleted>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

        await dbContext.RunInTransactionAsync(
            token => handler.HandleAsync(
                new FileDeleted { FileId = IngestAttemptTestData.FileId, CompanyId = IngestAttemptTestData.CompanyId },
                token),
            TestContext.Current.CancellationToken);

        scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>().Push();
    }
}
