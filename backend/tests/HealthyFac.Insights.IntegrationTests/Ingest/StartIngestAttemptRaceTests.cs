using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Application.Ingest;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Ingest;

[Collection(InsightsCollection.Name)]
public sealed class StartIngestAttemptRaceTests(InsightsFixture fixture)
{
    private const int RaceParticipantCount = 2;

    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    private static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task StartIngestAttempt_WhenTwoStartsOverlap_OneSucceedsAndOneConflicts()
    {
        _fixture.Files.Add(
            new IngestFile
            {
                FileId = FileId,
                CompanyId = CompanyId,
                MachineChangedAt = IngestAttemptTestData.Now,
                Purpose = IngestRules.AttachmentPurpose,
                FileName = "manual",
                Extension = "pdf",
                SizeBytes = 1024,
            });
        using var race = _fixture.SaveGate.StartRace(RaceParticipantCount);

        var failures = await Task.WhenAll(race.RunAsync(StartAsync), race.RunAsync(StartAsync));

        var attemptCount = await CountAttemptsAsync();

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(IngestErrors.IngestActive);
        attemptCount.Should().Be(expected: 1);
    }

    private async Task StartAsync()
    {
        using var scope = _fixture.CreateActorScope(
            new ActorContext { Kind = ActorKind.User, UserId = Guid.CreateVersion7(), CompanyId = CompanyId });
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<StartIngestAttemptRequest, StartIngestAttemptResponse>>();

        await handler.HandleAsync(new StartIngestAttemptRequest { CompanyId = CompanyId, FileId = FileId });
    }

    private async Task<int> CountAttemptsAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

        return await dbContext.IngestAttempts.CountAsync();
    }
}
