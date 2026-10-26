using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.Caching;

public sealed class RedisCircuitBreakerTests
{
    private const int ParallelCallerCount = 100;

    private readonly FakeTimeProvider _timeProvider = new();
    private readonly CapturingLogger<RedisCircuitBreaker> _logger = new();
    private readonly RedisCircuitBreaker _breaker;

    public RedisCircuitBreakerTests()
    {
        _breaker = new RedisCircuitBreaker(_timeProvider, _logger);
    }

    [Fact]
    public void CanTryRedis_WithinOpenPeriod_ReturnsFalse()
    {
        OpenBreaker();
        _timeProvider.Advance(RedisConstants.OpenPeriod - TimeSpan.FromSeconds(1));

        var canTryRedis = _breaker.CanTryRedis();

        canTryRedis.Should().BeFalse();
    }

    [Fact]
    public void CanTryRedis_AfterOpenPeriod_AllowsOneProbe()
    {
        OpenBreaker();
        _timeProvider.Advance(RedisConstants.OpenPeriod);

        var answers = new[] { _breaker.CanTryRedis(), _breaker.CanTryRedis() };

        answers.Should().Equal(true, false);
    }

    [Fact]
    public async Task CanTryRedis_AfterOpenPeriodWithParallelCallers_AllowsExactlyOneProbe()
    {
        OpenBreaker();
        _timeProvider.Advance(RedisConstants.OpenPeriod);

        var answers = await Task.WhenAll(
            Enumerable.Range(0, ParallelCallerCount).Select(_ => Task.Run(_breaker.CanTryRedis)));

        answers.Count(canTryRedis => canTryRedis).Should().Be(1);
    }

    [Fact]
    public void CanTryRedis_AfterProbeTimeout_AllowsNewProbe()
    {
        StartProbe();
        _timeProvider.Advance(RedisConstants.ProbeTimeout);

        var canTryRedis = _breaker.CanTryRedis();

        canTryRedis.Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_AfterProbe_ClosesBreaker()
    {
        StartProbe();
        _breaker.RecordSuccess();

        var answers = new[] { _breaker.CanTryRedis(), _breaker.CanTryRedis() };

        answers.Should().Equal(true, true);
    }

    [Fact]
    public void RecordSuccess_WhileOpen_KeepsBreakerOpen()
    {
        OpenBreaker();
        _breaker.RecordSuccess();

        var canTryRedis = _breaker.CanTryRedis();

        canTryRedis.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_AfterProbe_ReopensForOpenPeriod()
    {
        StartProbe();
        _breaker.RecordFailure(new TimeoutException());
        _timeProvider.Advance(RedisConstants.OpenPeriod - TimeSpan.FromSeconds(1));

        var canTryRedis = _breaker.CanTryRedis();

        canTryRedis.Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_WhenAlreadyOpen_LogsOneWarning()
    {
        OpenBreaker();
        _breaker.RecordFailure(new TimeoutException());

        var warnings = _logger.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();

        warnings.Should().ContainSingle();
    }

    [Fact]
    public void RecordSuccess_AfterProbe_LogsClosedWarning()
    {
        StartProbe();
        _breaker.RecordSuccess();

        var warnings = _logger.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();

        warnings.Should().HaveCount(2);
        warnings[1].Message.Should().Contain("closed");
    }

    [Fact]
    public void RecordFailure_AfterProbe_LogsNoNewWarning()
    {
        StartProbe();
        _breaker.RecordFailure(new TimeoutException());

        var warnings = _logger.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();

        warnings.Should().ContainSingle();
    }

    private void OpenBreaker() => _breaker.RecordFailure(new TimeoutException());

    private void StartProbe()
    {
        OpenBreaker();
        _timeProvider.Advance(RedisConstants.OpenPeriod);
        _breaker.CanTryRedis();
    }
}
