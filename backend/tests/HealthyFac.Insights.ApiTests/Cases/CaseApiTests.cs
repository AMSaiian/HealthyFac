using System.Net;
using System.Text.Json;
using HealthyFac.Insights.ApiTests.Cases.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.ApiTests.Cases;

[Collection(InsightsApiCollection.Name)]
[Feature(TestFeatures.Cases)]
public sealed class CaseApiTests(InsightsApiFixture fixture)
{
    private const string Reason = "No work is needed.";

    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_ReturnsTheCasesOfTheCompanyOnly()
    {
        var caseId = await _fixture.ArrangeCaseAsync(severity: SignalSeverity.Alarm);
        await _fixture.ArrangeCaseAsync(Guid.CreateVersion7());

        var response = await SearchAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var items = document.RootElement.GetProperty(propertyName: "items");
        items.GetArrayLength().Should().Be(expected: 1);
        items[0].GetProperty(propertyName: "id").GetString().Should().Be(_fixture.IdEncryptor.Encrypt(caseId));
        items[0].GetProperty(propertyName: "highestOpenSeverity").GetString().Should().Be(expected: "alarm");
        items[0].GetProperty(propertyName: "signalCount").GetInt32().Should().Be(expected: 1);
    }

    [Fact]
    public async Task Search_WithEncryptedMachineIds_ReturnsOnlyTheCasesOfThoseMachines()
    {
        var firstMachine = Guid.CreateVersion7();
        var secondMachine = Guid.CreateVersion7();
        var first = await _fixture.ArrangeCaseAsync(machineId: firstMachine);
        var second = await _fixture.ArrangeCaseAsync(machineId: secondMachine);
        await _fixture.ArrangeCaseAsync();

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.SearchPath,
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission),
            new
            {
                offset = 0,
                limit = 20,
                filter = new
                {
                    machineIds = new
                    {
                        ids = new[]
                        {
                            _fixture.IdEncryptor.Encrypt(firstMachine),
                            _fixture.IdEncryptor.Encrypt(secondMachine),
                        },
                    },
                },
            });

        var ids = await ReadIdsAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ids.Should().BeEquivalentTo(_fixture.IdEncryptor.Encrypt(first), _fixture.IdEncryptor.Encrypt(second));
    }

    [Fact]
    public async Task Search_WithoutFilter_ReturnsFirstPageOfAllRows()
    {
        var first = await _fixture.ArrangeCaseAsync();
        var second = await _fixture.ArrangeCaseAsync();

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.SearchPath,
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission),
            new { offset = 0, limit = 20 });

        var ids = await ReadIdsAsync(response);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var totalCount = document.RootElement.GetProperty(propertyName: "totalCount").GetInt32();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ids.Should().BeEquivalentTo(_fixture.IdEncryptor.Encrypt(first), _fixture.IdEncryptor.Encrypt(second));
        totalCount.Should().Be(expected: 2);
    }

    [Theory]
    [MemberData(nameof(CaseApiTestData.AiUnavailableCases), MemberType = typeof(CaseApiTestData))]
    public async Task Search_AfterAFailedRun_ShowsAiUnavailableOnlyForAProviderOrUnexpectedFailure(
        string caseName,
        AgentRunErrorCode errorCode,
        bool isAiUnavailable
    )
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(caseId, errorCode);

        var response = await SearchAsync();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var item = document.RootElement.GetProperty(propertyName: "items")[0];
        item.GetProperty(propertyName: "isAiUnavailable").GetBoolean().Should().Be(isAiUnavailable, caseName);
        item.GetProperty(propertyName: "lastRunState").GetString().Should().Be(expected: "failed");
    }

    [Fact]
    public async Task Get_ReturnsTheHeaderWithTheOutcomeAndNoSignalsOrRuns()
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(caseId, AgentRunErrorCode.ProviderUnavailable);

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Get,
            CaseApiTestData.ByIdPath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        document.RootElement.GetProperty(propertyName: "signalCount").GetInt32().Should().Be(expected: 1);

        document.RootElement.GetProperty(propertyName: "outcome")
            .GetProperty(propertyName: "kind")
            .GetString()
            .Should()
            .Be(expected: "aiUnavailable");

        document.RootElement.TryGetProperty(propertyName: "signals", out _).Should().BeFalse();
        document.RootElement.TryGetProperty(propertyName: "runs", out _).Should().BeFalse();
    }

    [Fact]
    public async Task SearchSignals_ReturnsTheSignalsOfTheCase()
    {
        var caseId = await _fixture.ArrangeCaseAsync();

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.SignalsSearchPath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission),
            new { offset = 0, limit = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        document.RootElement.GetProperty(propertyName: "totalCount").GetInt32().Should().Be(expected: 1);

        document.RootElement.GetProperty(propertyName: "items")[0]
            .GetProperty(propertyName: "coverage")
            .GetString()
            .Should()
            .Be(expected: "new");
    }

    [Fact]
    public async Task SearchRuns_ReturnsTheRunsWithoutToolCalls()
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(caseId, AgentRunErrorCode.ProviderUnavailable);

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.RunsSearchPath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission),
            new { offset = 0, limit = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var run = document.RootElement.GetProperty(propertyName: "items")[0];
        run.GetProperty(propertyName: "errorCode").GetString().Should().Be(expected: "providerUnavailable");
        run.TryGetProperty(propertyName: "toolCalls", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Get_ForACaseOfAnotherCompany_Returns404()
    {
        var caseId = await _fixture.ArrangeCaseAsync(Guid.CreateVersion7());

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Get,
            CaseApiTestData.ByIdPath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RunAgain_ForAnOpenCase_QueuesOneTriageRunDueNow()
    {
        var caseId = await _fixture.ArrangeCaseAsync();

        var response = await RunAgainAsync(caseId);
        var runs = await _fixture.LoadCaseRunsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var run = runs.Should().ContainSingle().Subject;
        run.Role.Should().Be(AgentRole.Triage);
        run.State.Should().Be(AgentRunState.Queued);
        run.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task RunAgain_ForAClosedCase_Returns422AndQueuesNothing()
    {
        var caseId = await _fixture.ArrangeCaseAsync(isClosed: true);

        var response = await RunAgainAsync(caseId);
        var runs = await _fixture.LoadCaseRunsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task Close_ForAnOpenCase_ClosesItWithTheReasonAndDismissesTheSignals()
    {
        var caseId = await _fixture.ArrangeCaseAsync();

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.ClosePath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.WritePermission),
            new { reason = Reason });

        var maintenanceCase = await _fixture.LoadCaseAsync(caseId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        maintenanceCase.State.Should().Be(CaseState.Closed);
        maintenanceCase.CloseReason.Should().Be(Reason);
        maintenanceCase.Signals.Should().OnlyContain(signal => signal.Coverage == SignalCoverage.Dismissed);
    }

    [Fact]
    public async Task Close_ForAClosedCase_Returns422()
    {
        var caseId = await _fixture.ArrangeCaseAsync(isClosed: true);

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.ClosePath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.WritePermission),
            new { reason = Reason });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Search_WithASeverityFilter_ReturnsOnlyTheCasesWhoseHighestOpenSeverityIsInTheSet()
    {
        await _fixture.ArrangeCaseAsync(severity: SignalSeverity.Warning);
        var alarmCaseId = await _fixture.ArrangeCaseAsync(severity: SignalSeverity.Alarm);

        var response = await SearchAsync(new { severities = new { values = new[] { "alarm" } } });

        (await ReadIdsAsync(response)).Should().Equal(_fixture.IdEncryptor.Encrypt(alarmCaseId));
    }

    [Fact]
    public async Task Search_WithAnAiUnavailableFilter_SplitsTheCasesByTheRuleOfTheRowField()
    {
        var failedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(failedCaseId, AgentRunErrorCode.ProviderUnavailable);
        var changedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(changedCaseId, AgentRunErrorCode.CaseChanged);
        var withoutRunCaseId = await _fixture.ArrangeCaseAsync();

        var unavailable = await SearchAsync(new { isAiUnavailable = true });
        var available = await SearchAsync(new { isAiUnavailable = false });

        (await ReadIdsAsync(unavailable)).Should().Equal(_fixture.IdEncryptor.Encrypt(failedCaseId));

        (await ReadIdsAsync(available))
            .Should()
            .BeEquivalentTo(
                _fixture.IdEncryptor.Encrypt(changedCaseId), _fixture.IdEncryptor.Encrypt(withoutRunCaseId));
    }

    [Fact]
    public async Task Search_WithALastRunStatesFilter_KeepsOnlyTheCasesWhoseLastRunHasAPickedState()
    {
        var completedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeRunAsync(completedCaseId, AgentRunState.Completed);
        var skippedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeRunAsync(skippedCaseId, AgentRunState.Skipped);
        await _fixture.ArrangeCaseAsync();

        var response = await SearchAsync(new { lastRunStates = new { values = new[] { "skipped", "failed" } } });

        (await ReadIdsAsync(response)).Should().Equal(_fixture.IdEncryptor.Encrypt(skippedCaseId));
    }

    [Fact]
    public async Task Search_WithALastRunStatesFilter_IgnoresTheOlderRunsOfACase()
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeRunAsync(caseId, AgentRunState.Completed);
        await _fixture.ArrangeRunAsync(caseId, AgentRunState.Skipped);

        var completed = await SearchAsync(new { lastRunStates = new { values = new[] { "completed" } } });
        var skipped = await SearchAsync(new { lastRunStates = new { values = new[] { "skipped" } } });

        (await ReadIdsAsync(completed)).Should().BeEmpty();
        (await ReadIdsAsync(skipped)).Should().Equal(_fixture.IdEncryptor.Encrypt(caseId));
    }

    [Fact]
    public async Task Search_WithLastRunStatesAndAiUnavailable_CombinesThemWithAnd()
    {
        var failedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(failedCaseId, AgentRunErrorCode.ProviderUnavailable);
        var changedCaseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeFailedRunAsync(changedCaseId, AgentRunErrorCode.CaseChanged);

        var both = await SearchAsync(
            new { isAiUnavailable = true, lastRunStates = new { values = new[] { "failed" } } });

        var contradicting = await SearchAsync(
            new { isAiUnavailable = true, lastRunStates = new { values = new[] { "completed" } } });

        (await ReadIdsAsync(both)).Should().Equal(_fixture.IdEncryptor.Encrypt(failedCaseId));
        (await ReadIdsAsync(contradicting)).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_AfterASkippedRun_DoesNotShowAiUnavailable()
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        await _fixture.ArrangeRunAsync(caseId, AgentRunState.Skipped);

        var response = await SearchAsync();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var item = document.RootElement.GetProperty(propertyName: "items")[0];
        item.GetProperty(propertyName: "isAiUnavailable").GetBoolean().Should().BeFalse();
        item.GetProperty(propertyName: "lastRunState").GetString().Should().Be(expected: "skipped");
    }

    [Fact]
    public async Task Routes_WithoutTheirPermission_Return403()
    {
        var caseId = await _fixture.ArrangeCaseAsync();
        var encryptedId = _fixture.IdEncryptor.Encrypt(caseId);
        var readOnly = _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission);
        var writeOnly = _fixture.CreateCaseClaims(CaseApiTestData.WritePermission);

        var search = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.SearchPath,
            writeOnly,
            new { offset = 0, limit = 20 });

        var get = await _fixture.SendCaseRequestAsync(
            HttpMethod.Get,
            CaseApiTestData.ByIdPath(encryptedId),
            writeOnly);

        var run = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.RunsPath(encryptedId),
            readOnly);

        var close = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.ClosePath(encryptedId),
            readOnly,
            new { reason = Reason });

        new[] { search.StatusCode, get.StatusCode, run.StatusCode, close.StatusCode }
            .Should()
            .OnlyContain(status => status == HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> SearchAsync(object? filter = null) =>
        _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.SearchPath,
            _fixture.CreateCaseClaims(CaseApiTestData.ReadPermission),
            new { offset = 0, limit = 20, filter = filter ?? new { } });

    private static async Task<IReadOnlyList<string>> ReadIdsAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        return
        [
            .. document.RootElement.GetProperty(propertyName: "items")
                .EnumerateArray()
                .Select(item => item.GetProperty(propertyName: "id").GetString()!),
        ];
    }

    private Task<HttpResponseMessage> RunAgainAsync(Guid caseId) =>
        _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            CaseApiTestData.RunsPath(_fixture.IdEncryptor.Encrypt(caseId)),
            _fixture.CreateCaseClaims(CaseApiTestData.WritePermission));
}
