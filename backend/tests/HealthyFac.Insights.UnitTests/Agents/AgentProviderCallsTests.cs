using System.Net;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Agents.Models;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Agents;

[Feature(TestFeatures.Cases)]
public sealed class AgentProviderCallsTests
{
    private readonly FakeTimeProvider _time = new(AgentProviderCallsTestData.Start);
    private readonly AgentProviderCalls _calls;

    public AgentProviderCallsTests()
    {
        _calls = new AgentProviderCalls(
            Options.Create(new AgentOptions { ProviderRetryDelaysSeconds = AgentProviderCallsTestData.Delays }),
            _time);
    }

    [Fact]
    public async Task ExecuteAsync_WithATransientErrorEveryTime_TriesFourTimesAfterTheDelaysThenFails()
    {
        var callTimes = new List<DateTimeOffset>();

        var task = _calls.ExecuteAsync<int>(
            _ =>
            {
                callTimes.Add(_time.GetUtcNow());

                throw AgentProviderCallsTestData.Transient();
            },
            TestContext.Current.CancellationToken);

        await DriveAsync(task);

        var act = () => task;
        await act.Should().ThrowAsync<ProviderUnavailableException>();

        callTimes.Select(time => (time - AgentProviderCallsTestData.Start).TotalSeconds)
            .Should()
            .Equal(0, 10, 40, 130);
    }

    [Fact]
    public async Task ExecuteAsync_WithASuccessAtTheThirdCall_StopsTheRetries()
    {
        var calls = 0;

        var task = _calls.ExecuteAsync(
            _ =>
            {
                calls++;

                return calls < 3 ? throw AgentProviderCallsTestData.Transient() : Task.FromResult(calls);
            },
            TestContext.Current.CancellationToken);

        await DriveAsync(task);

        var result = await task;
        result.Should().Be(expected: 3);
        calls.Should().Be(expected: 3);
    }

    [Fact]
    public async Task ExecuteAsync_WithAPermanentError_FailsAtOnceWithoutRetry()
    {
        var calls = 0;

        var task = _calls.ExecuteAsync<int>(
            _ =>
            {
                calls++;

                throw new HttpRequestException(
                    AgentProviderCallsTestData.ProviderText, inner: null, HttpStatusCode.Unauthorized);
            },
            TestContext.Current.CancellationToken);

        await DriveAsync(task);

        var act = () => task;
        var thrown = await act.Should().ThrowAsync<ProviderUnavailableException>();
        calls.Should().Be(expected: 1);
        thrown.Which.Message.Should().NotContain(AgentProviderCallsTestData.ProviderText);
    }

    private async Task DriveAsync(Task task)
    {
        for (var step = 0; step < AgentProviderCallsTestData.MaxSteps && !task.IsCompleted; step++)
        {
            _time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.Yield();
        }
    }
}
