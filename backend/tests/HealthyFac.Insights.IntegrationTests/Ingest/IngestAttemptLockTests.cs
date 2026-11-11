using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Insights.IntegrationTests.Documents.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class IngestAttemptLockTests(InsightsFixture fixture)
{
    private static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task LockActiveAttempt_WhenTheTransactionTrackedTheRowBefore_ReturnsTheCommittedValues()
    {
        await _fixture.SaveAsync(IngestAttemptTestData.CreateQueued());
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();
        var attemptLock = scope.ServiceProvider.GetRequiredService<IIngestAttemptLock>();

        var locked = await dbContext.RunInTransactionAsync(
            token => TrackChangeElsewhereThenLockAsync(dbContext, attemptLock, token),
            TestContext.Current.CancellationToken);

        locked!.MachineId.Should().Be(MachineId);
    }

    private async Task<Domain.Ingest.IngestAttempt?> TrackChangeElsewhereThenLockAsync(
        IInsightsDbContext dbContext,
        IIngestAttemptLock attemptLock,
        CancellationToken cancellationToken
    )
    {
        await dbContext.IngestAttempts.SingleAsync(cancellationToken);
        await _fixture.HandleMachineChangedAsync(
            IngestAttemptTestData.CompanyId,
            IngestAttemptTestData.FileId,
            MachineId,
            IngestAttemptTestData.Now.AddMinutes(value: 5));

        return await attemptLock.LockActiveAttemptAsync(
            IngestAttemptTestData.FileId, IngestAttemptTestData.CompanyId, cancellationToken);
    }
}
