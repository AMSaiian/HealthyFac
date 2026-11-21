using System.Diagnostics;
using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.BackgroundQueues;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Shared.UnitTests.Realtime.Data;

public sealed class FakeRealtimeClient : IRealtimeClient
{
    private int _sendCount;

    public int SendCount => Volatile.Read(ref _sendCount);

    public Exception? Failure { get; set; }

    public List<string> Targets { get; } = [];

    public Task Notification(RealtimeNotification notification) => Send(nameof(Notification));

    public Task Hint(RealtimeHint hint) => Send(nameof(Hint));

    public Task SessionEnded() => Send(nameof(SessionEnded));

    private Task Send(string method)
    {
        Interlocked.Increment(ref _sendCount);
        Targets.Add(method);

        return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }
}

public sealed class FakeHubClients(FakeRealtimeClient client) : IHubClients<IRealtimeClient>
{
    private readonly FakeRealtimeClient _client = client;

    public List<string> UserCalls { get; } = [];

    public List<string> GroupCalls { get; } = [];

    public IRealtimeClient User(string userId)
    {
        UserCalls.Add(userId);

        return _client;
    }

    public IRealtimeClient Group(string groupName)
    {
        GroupCalls.Add(groupName);

        return _client;
    }

    public IRealtimeClient All => _client;

    public IRealtimeClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => _client;

    public IRealtimeClient Client(string connectionId) => _client;

    public IRealtimeClient Clients(IReadOnlyList<string> connectionIds) => _client;

    public IRealtimeClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _client;

    public IRealtimeClient Groups(IReadOnlyList<string> groupNames) => _client;

    public IRealtimeClient Users(IReadOnlyList<string> userIds) => _client;
}

public sealed class FakeHubContext(FakeHubClients clients) : IHubContext<RealtimeHub, IRealtimeClient>
{
    public IHubClients<IRealtimeClient> Clients { get; } = clients;

    public IGroupManager Groups => throw new NotSupportedException();
}

public sealed class AttemptCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}

public sealed class CountingStrategyOptions : ResilienceStrategyOptions;

public sealed class CountingStrategy(AttemptCounter counter) : ResilienceStrategy
{
    private readonly AttemptCounter _counter = counter;

    protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
        ResilienceContext context,
        TState state
    )
    {
        _counter.Increment();

        return callback(context, state);
    }
}

public sealed class PushHandlerFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public PushHandlerFixture(ResiliencePipeline? redisPipeline = null)
    {
        _provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        Client = new FakeRealtimeClient();
        Clients = new FakeHubClients(Client);
        Time = new FakeTimeProvider(RealtimePushHandlerTestData.Now);
        Logger = new FakeLogger<RealtimePushHandler>();
        Queue = new BackgroundQueue<RealtimePush>(BackgroundQueueCapacity.Unbounded);

        var retryBuilder = new ResiliencePipelineBuilder();
        RealtimeExtensions.ConfigurePushPipeline(retryBuilder, Time);
        var queueProvider = new ServiceCollection().AddSingleton(Queue).BuildServiceProvider();
        Metrics = new RealtimeMetrics(MeterFactory, queueProvider);
        Handler = new RealtimePushHandler(
            new FakeHubContext(Clients),
            new RedisResiliencePipeline(redisPipeline ?? ResiliencePipeline.Empty, new CircuitBreakerStateProvider()),
            retryBuilder.Build(),
            Metrics,
            Time,
            Logger);
    }

    public FakeRealtimeClient Client { get; }

    public FakeHubClients Clients { get; }

    public FakeTimeProvider Time { get; }

    public FakeLogger<RealtimePushHandler> Logger { get; }

    public BackgroundQueue<RealtimePush> Queue { get; }

    public RealtimeMetrics Metrics { get; }

    public RealtimePushHandler Handler { get; }

    public IMeterFactory MeterFactory => _provider.GetRequiredService<IMeterFactory>();

    public MetricCollector<long> CollectPushes() =>
        new(MeterFactory, meterName: "HealthyFac.Realtime", instrumentName: "healthyfac.realtime.pushes");

    public MetricCollector<double> CollectDurations() =>
        new(MeterFactory, meterName: "HealthyFac.Realtime", instrumentName: "healthyfac.realtime.push.duration");

    public MetricCollector<int> CollectQueueLength() =>
        new(
            MeterFactory, meterName: "HealthyFac.BackgroundQueues",
            instrumentName: "healthyfac.background_queue.length");

    /// <summary>
    /// Runs the handler and moves the fake clock forward until it ends, because the retry delays wait on that clock.
    /// </summary>
    public async Task HandleAsync(RealtimePush push, ActivityContext parentContext = default(ActivityContext))
    {
        var handling = Handler.HandleAsync(push, parentContext, TestContext.Current.CancellationToken);
        while (!handling.IsCompleted)
        {
            Time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 5), TestContext.Current.CancellationToken);
        }

        await handling;
    }

    public void Dispose() => _provider.Dispose();
}

public static class RealtimePushHandlerTestData
{
    /// <summary>The three retry delays of the push without jitter: 200, 400, and 800 milliseconds.</summary>
    public const double MinimumRetrySeconds = 1.4;

    public static readonly DateTimeOffset Now = new(
        year: 2026, month: 11, day: 20, hour: 10, minute: 0, second: 0, TimeSpan.Zero);

    public static readonly Guid UserId = Guid.Parse(input: "55555555-5555-5555-5555-555555555555");

    public static readonly ActivityContext ParentContext = new(
        ActivityTraceId.CreateFromString(idData: "0af7651916cd43dd8448eb211c80319c"),
        ActivitySpanId.CreateFromString(idData: "b7ad6b7169203331"),
        ActivityTraceFlags.Recorded);

    public static RealtimePush UserPush() =>
        new()
        {
            Target = RealtimePushTarget.User,
            UserId = UserId,
            Notification = new RealtimeNotification
            {
                NotificationId = Guid.NewGuid(),
                Kind = RealtimeNotificationKind.ShiftChanged,
                OccurredAt = Now,
                Data = new { Name = "data" },
            },
            MetricTarget = RealtimeNotificationKind.ShiftChanged.Value,
        };

    public static Exception Failure => new RedisTimeoutException(message: "down", CommandStatus.Unknown);
}
