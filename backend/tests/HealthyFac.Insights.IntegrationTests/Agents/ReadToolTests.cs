using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Reads;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using HealthyFac.Insights.IntegrationTests.Cases;
using HealthyFac.Insights.IntegrationTests.Cases.Data;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.IntegrationTests.Agents;

[Collection(SignalCaseCollection.Name)]
public sealed class ReadToolTests(SignalCaseFixture fixture)
{
    private const string OwnText = "Replace the filter every 500 hours.";
    private const string ForeignMachineText = "Foreign machine passage.";
    private const string ForeignCompanyText = "Foreign company passage.";
    private const string AssigneeWord = "assignee";
    private const int SurplusChunkCount = 3;
    private const int OverRangeHours = 25;

    private const string MetricKey = "pump.vibration";

    private static readonly Guid MetricId = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");
    private static readonly Guid ForeignGuid = Guid.Parse(input: "11111111-1111-1111-1111-111111111111");
    private static readonly Guid ForeignCompanyId = Guid.Parse(input: "22222222-2222-2222-2222-222222222222");

    private readonly SignalCaseFixture _fixture = fixture;

    [Fact]
    public async Task ListOpenWorkOrders_ThroughTheTool_AsksForTheWorkOrdersScopeAndShowsNoAssignee()
    {
        await ArrangeCaseAsync();
        _fixture.Agents.WorkOrders.Answer.Add(
            new OpenWorkOrderInfo
            {
                Id = Guid.CreateVersion7(),
                Title = "Replace the filter",
                Status = "staged",
                Priority = "high",
                Category = "repair",
            });

        var result = await _fixture.CallToolAsync(ListOpenWorkOrdersToolRequest.ToolName);

        var call = _fixture.Agents.WorkOrders.Calls.Should().ContainSingle().Subject;
        call.Scopes.Should().BeEquivalentTo("work-orders");
        call.Limit.Should().Be(Options().OpenWorkOrderCount);
        result.Should().Contain(expected: "Replace the filter").And.NotContainEquivalentOf(AssigneeWord);
    }

    [Fact]
    public async Task SearchManual_WithManualsOfOtherMachinesAndCompanies_ReturnsOnlyTheChunksOfTheCaseMachine()
    {
        await ArrangeCaseAsync();
        await _fixture.SeedManualAsync(CaseTestData.CompanyId, CaseTestData.MachineId, OwnText);
        await _fixture.SeedManualAsync(CaseTestData.CompanyId, CaseTestData.OtherMachineId, ForeignMachineText);
        await _fixture.SeedManualAsync(ForeignCompanyId, CaseTestData.MachineId, ForeignCompanyText);

        var result = await _fixture.CallToolAsync(
            SearchManualRequest.ToolName, argumentsJson: """{"query":"filter"}""");

        result.Should().Contain(expected: "<manual_chunk>").And.Contain(OwnText).And.Contain(expected: "page 1");
        result.Should().NotContain(ForeignMachineText).And.NotContain(ForeignCompanyText);
    }

    [Fact]
    public async Task SearchManual_WithMoreChunksThanTheOption_ReturnsTheOptionCount()
    {
        await ArrangeCaseAsync();
        var texts = Enumerable.Range(start: 1, Options().ManualSearchChunks + SurplusChunkCount)
            .Select(number => $"Passage {number}.")
            .ToArray();
        await _fixture.SeedManualAsync(CaseTestData.CompanyId, CaseTestData.MachineId, texts);

        var result = await _fixture.CallToolAsync(
            SearchManualRequest.ToolName, argumentsJson: """{"query":"passage"}""");

        result.Split(separator: "<manual_chunk>").Length.Should().Be(Options().ManualSearchChunks + 1);
    }

    [Fact]
    public async Task GetMetricHistory_WithARangeAboveTheOption_IsRejectedWithoutACallToTelemetry()
    {
        await ArrangeCaseAsync();
        var to = _fixture.Time.GetUtcNow().UtcDateTime;
        var arguments = $$"""
                          {"metricKey":"{{MetricKey}}",
                           "from":"{{to.AddHours(-OverRangeHours):O}}","to":"{{to:O}}"}
                          """;

        ArrangeMetric();

        await _fixture.CallToolAsync(GetMetricHistoryToolRequest.ToolName, arguments);

        var call = (await _fixture.LoadToolCallsAsync()).Single();
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
        _fixture.Agents.Telemetry.HistoryCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricHistory_WithoutARange_ReadsTheDefaultRangeInTheBucketCountOfTheOption()
    {
        await ArrangeCaseAsync();
        ArrangeMetric();

        await _fixture.CallToolAsync(
            GetMetricHistoryToolRequest.ToolName,
            $$"""{"metricKey":"{{MetricKey}}"}""");

        var call = _fixture.Agents.Telemetry.HistoryCalls.Should().ContainSingle().Subject;
        call.MetricId.Should().Be(MetricId);
        call.BucketCount.Should().Be(Options().PlannerHistoryBucketCount);
        (call.To - call.From).Should().Be(TimeSpan.FromMinutes(Options().PlannerHistoryMinutes));
        call.MachineId.Should().Be(CaseTestData.MachineId);
    }

    [Fact]
    public async Task GetMetricHistory_WithAKeyThatNoActiveMetricHas_IsRejectedWithoutACallToTelemetry()
    {
        await ArrangeCaseAsync();
        ArrangeMetric();

        await _fixture.CallToolAsync(
            GetMetricHistoryToolRequest.ToolName,
            argumentsJson: """{"metricKey":"no.such.key"}""");

        var call = (await _fixture.LoadToolCallsAsync()).Single();
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
        call.Result.Should().Contain(AgentMessages.UnknownMetricKey);
        _fixture.Agents.Telemetry.HistoryCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchManual_InTheSameRun_ReturnsRepeatOfForAPassageThatAnEarlierCallReturned()
    {
        await ArrangeCaseAsync();
        await _fixture.SeedManualAsync(CaseTestData.CompanyId, CaseTestData.MachineId, OwnText, "Another passage.");
        var runId = await _fixture.AddRunningRunAsync((await _fixture.LoadCasesAsync()).Single().Id);

        var log = new ManualPassageLog();

        var first = await _fixture.CallToolAsync(
            SearchManualRequest.ToolName,
            argumentsJson: """{"query":"filter"}""",
            existingRunId: runId,
            manualPassages: log);
        var second = await _fixture.CallToolAsync(
            SearchManualRequest.ToolName,
            argumentsJson: """{"query":"passage"}""",
            existingRunId: runId,
            manualPassages: log);

        first.Should().Contain(OwnText).And.NotContain(unexpected: "repeatOf");
        second.Should().Contain(expected: "\"repeatOf\": 1").And.NotContain(OwnText);
    }

    [Fact]
    public async Task SearchManual_WithTitleOnlyChunks_NeverReturnsThemAndStillFillsTheOptionCount()
    {
        await ArrangeCaseAsync();
        var titleOnly = new[] { "**HALVIK PUMPS**", "# 7 Operation", "# 7 Operation\n**Monitoring**" };
        var texts = titleOnly
            .Concat(Enumerable.Range(start: 1, Options().ManualSearchChunks).Select(number => $"Passage {number}."))
            .ToArray();
        await _fixture.SeedManualAsync(CaseTestData.CompanyId, CaseTestData.MachineId, texts);

        var result = await _fixture.CallToolAsync(
            SearchManualRequest.ToolName, argumentsJson: """{"query":"passage"}""");

        result.Should().NotContain(unexpected: "HALVIK PUMPS").And.NotContain(unexpected: "# 7 Operation");
        result.Split(separator: "<manual_chunk>").Length.Should().Be(Options().ManualSearchChunks + 1);
    }

    [Fact]
    public async Task GetSignalEpisodes_WithMoreEpisodesThanTheOption_ReturnsTheNewestOnesInTheRangeOfTheOption()
    {
        await ArrangeCaseAsync();
        var now = _fixture.Time.GetUtcNow().UtcDateTime;
        _fixture.Agents.Telemetry.Episodes.AddRange(
            Enumerable.Range(start: 0, Options().EpisodeCount + SurplusChunkCount)
                .Select(number => Episode(now.AddMinutes(-number))));

        var result = await _fixture.CallToolAsync(GetSignalEpisodesToolRequest.ToolName);

        var call = _fixture.Agents.Telemetry.EpisodeCalls.Should().ContainSingle().Subject;
        (call.To - call.From).Should().Be(TimeSpan.FromHours(Options().EpisodeRangeHours));
        result.Split(separator: "\"episodeId\"").Length.Should().Be(Options().EpisodeCount + 1);
    }

    [Fact]
    public async Task Call_WithAllFields_IsStoredAtOnce()
    {
        await ArrangeCaseAsync();

        var result = await _fixture.CallToolAsync(ListOpenWorkOrdersToolRequest.ToolName);

        var call = (await _fixture.LoadToolCallsAsync()).Should().ContainSingle().Subject;
        call.Sequence.Should().Be(expected: 1);
        call.CallId.Should().NotBeNullOrWhiteSpace();
        call.ToolName.Should().Be(ListOpenWorkOrdersToolRequest.ToolName);
        call.Arguments.Should().Be(CaseTestData.EmptyJson);
        call.Outcome.Should().Be(ToolCallOutcome.Succeeded);
        call.Result.Should().Contain(expected: "workOrders");
        call.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(expected: 0);
        result.Should().StartWith(expected: "<tool_result>");
    }

    [Fact]
    public async Task Call_WithACompanyAndMachineInTheArguments_ReadsTheCaseMachine()
    {
        await ArrangeCaseAsync();

        await _fixture.CallToolAsync(
            ListOpenWorkOrdersToolRequest.ToolName,
            $$"""{"companyId":"{{ForeignCompanyId}}","machineId":"{{ForeignGuid}}"}""");

        var call = _fixture.Agents.WorkOrders.Calls.Should().ContainSingle().Subject;
        call.MachineId.Should().Be(CaseTestData.MachineId);
    }

    [Fact]
    public async Task Call_ForAMachineWithoutACaseOfTheCompany_IsRejectedBeforeTheRead()
    {
        await ArrangeCaseAsync();

        await _fixture.CallToolAsync(ListOpenWorkOrdersToolRequest.ToolName, machineId: ForeignGuid);

        var call = (await _fixture.LoadToolCallsAsync()).Single();
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
        call.Result.Should().Contain(ToolMachineScope.MachineNotFound);
        _fixture.Agents.WorkOrders.Calls.Should().BeEmpty();
    }

    private static SignalEpisodeInfo Episode(DateTime openedAt) =>
        new()
        {
            EpisodeId = Guid.CreateVersion7(),
            MachineId = CaseTestData.MachineId,
            CompanyId = CaseTestData.CompanyId,
            Rule = "threshold",
            Severity = "warning",
            State = "open",
            OpenedAt = openedAt,
            Sequence = 1,
        };

    private AgentOptions Options() => _fixture.Inner.GetSingleton<IOptions<AgentOptions>>().Value;

    private void ArrangeMetric()
    {
        _fixture.Inner.Ingest.Signals.Signals =
            new HealthyFac.Insights.Application.Extraction.Signals.MachineSignals
            {
                Metrics =
                [
                    new HealthyFac.Insights.Application.Extraction.Signals.SignalReference
                    {
                        Id = MetricId,
                        Key = MetricKey,
                    },
                ],
                Statuses = [],
            };
    }

    private async Task ArrangeCaseAsync()
    {
        await _fixture.HandleSignalAsync(CaseTestData.Message(CaseTestData.FirstEpisodeId, sequence: 1));
    }
}
