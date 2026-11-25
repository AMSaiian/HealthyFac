using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.IntegrationTests.Ingest.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Agents.Data;

/// <summary>
/// The doubles of the models and the services that the agent runs call. A test scripts the answers, and the fixture
/// resets the doubles before the next test.
/// </summary>
public sealed class AgentFakes
{
    /// <summary>The scripted model behind the keyed triage chat client.</summary>
    public ScriptedChatClient TriageChat { get; } = new();

    public FakeTelemetrySignals Telemetry { get; } = new();

    public FakeWorkOrderCatalog WorkOrders { get; } = new();

    public void Reset()
    {
        TriageChat.Reset();
        Telemetry.Reset();
        WorkOrders.Reset();
    }

    public void Register(IServiceCollection services)
    {
        services.AddKeyedSingleton<IChatClient>(AiClientKeys.Triage, TriageChat);
        services.AddSingleton<ITelemetrySignals>(Telemetry);
        services.AddScoped<IAgentRunner, ThrowingScreenerRunner>();

        services.AddScoped<IWorkOrderCatalog>(provider => new ScopedWorkOrderCatalog(
            WorkOrders, provider.GetRequiredService<IDownstreamScopeHolder>()));
    }

    private sealed class ScopedWorkOrderCatalog(FakeWorkOrderCatalog shared, IDownstreamScopeHolder scopes)
        : IWorkOrderCatalog
    {
        public Task<IReadOnlyList<OpenWorkOrderInfo>> ListOpenAsync(
            Guid machineId,
            int limit,
            CancellationToken cancellationToken
        )
        {
            shared.Calls.Add(new WorkOrderCall(machineId, limit, [.. scopes.Current]));

            if (shared.Failure is not null)
            {
                throw shared.Failure;
            }

            return Task.FromResult<IReadOnlyList<OpenWorkOrderInfo>>(shared.Answer);
        }
    }
}

/// <summary>
/// A runner of the screener role that buffers one action, saves it, and then throws, so a test sees what the run
/// handler does with an unexpected error.
/// </summary>
/// <param name="dbContext">Saves the buffered action.</param>
public sealed class ThrowingScreenerRunner(IInsightsDbContext dbContext) : IAgentRunner
{
    public const string ErrorMessage = "The runner broke.";

    public AgentRole Role => AgentRole.Screener;

    public async Task RunAsync(AgentRun run, MaintenanceCase maintenanceCase, CancellationToken cancellationToken)
    {
        dbContext.AgentActions.Add(new AgentAction(run.Id, callId: "call-1", AgentActionKind.CloseCase, payload: "{}"));
        await dbContext.SaveChangesAsync(cancellationToken);

        throw new InvalidOperationException(ErrorMessage);
    }
}

public sealed record WorkOrderCall(Guid MachineId, int Limit, IReadOnlyList<string> Scopes);

public sealed class FakeWorkOrderCatalog
{
    public List<WorkOrderCall> Calls { get; } = [];

    public List<OpenWorkOrderInfo> Answer { get; } = [];

    /// <summary>The exception that the next calls throw. <see langword="null"/> means they answer.</summary>
    public Exception? Failure { get; set; }

    public void Reset()
    {
        Calls.Clear();
        Answer.Clear();
        Failure = null;
    }
}

public sealed class FakeTelemetrySignals : ITelemetrySignals
{
    public List<(Guid MachineId, Guid MetricId, DateTime From, DateTime To, int BucketCount)> HistoryCalls { get; } =
        [];

    public List<(Guid MachineId, DateTime From, DateTime To)> EpisodeCalls { get; } = [];

    public List<SignalEpisodeInfo> Episodes { get; } = [];

    public void Reset()
    {
        HistoryCalls.Clear();
        EpisodeCalls.Clear();
        Episodes.Clear();
    }

    public Task<IReadOnlyList<MetricHistoryBucket>> GetMetricHistoryAsync(
        Guid machineId,
        Guid metricId,
        DateTime from,
        DateTime to,
        int bucketCount,
        CancellationToken cancellationToken
    )
    {
        HistoryCalls.Add((machineId, metricId, from, to, bucketCount));

        return Task.FromResult<IReadOnlyList<MetricHistoryBucket>>([]);
    }

    public Task<IReadOnlyList<SignalEpisodeInfo>> GetSignalEpisodesAsync(
        Guid machineId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        EpisodeCalls.Add((machineId, from, to));

        return Task.FromResult<IReadOnlyList<SignalEpisodeInfo>>([.. Episodes]);
    }
}

/// <summary>
/// Plays the reads of the chat tools. A test scripts the answer or the failure, and reads back the arguments that the
/// tool passed. A read that no test scripts throws, so a test never reads data by accident.
/// </summary>
public sealed class FakeChatReads : IChatReads
{
    public List<object> Calls { get; } = [];

    public SearchMachinesResponse? MachinesAnswer { get; set; }

    public GetWorkOrderResponse? WorkOrderAnswer { get; set; }

    public Exception? Failure { get; set; }

    public void Reset()
    {
        Calls.Clear();
        MachinesAnswer = null;
        WorkOrderAnswer = null;
        Failure = null;
    }

    public Task<SearchMachinesResponse> SearchMachinesAsync(
        SearchMachinesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer(arguments, MachinesAnswer);

    public Task<GetWorkOrderResponse> GetWorkOrderAsync(
        GetWorkOrderArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer(arguments, WorkOrderAnswer);

    public Task<GetMachineResponse> GetMachineAsync(
        GetMachineArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetMachineResponse>(arguments, answer: null);

    public Task<GetMachineStateTimesResponse> GetMachineStateTimesAsync(
        GetMachineStateTimesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetMachineStateTimesResponse>(arguments, answer: null);

    public Task<GetMetricHistoryResponse> GetMetricHistoryAsync(
        GetMetricHistoryArguments arguments,
        int bucketCount,
        CancellationToken cancellationToken
    ) =>
        Answer<GetMetricHistoryResponse>(arguments, answer: null);

    public Task<GetSignalEpisodesResponse> GetSignalEpisodesAsync(
        GetSignalEpisodesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetSignalEpisodesResponse>(arguments, answer: null);

    public Task<GetPhaseTimesResponse> GetPhaseTimesAsync(
        GetPhaseTimesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetPhaseTimesResponse>(arguments, answer: null);

    public Task<SearchWorkOrdersResponse> SearchWorkOrdersAsync(
        SearchWorkOrdersArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<SearchWorkOrdersResponse>(arguments, answer: null);

    public Task<SearchEmployeesResponse> SearchEmployeesAsync(
        SearchEmployeesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<SearchEmployeesResponse>(arguments, answer: null);

    public Task<GetScheduleResponse> GetScheduleAsync(
        GetScheduleArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetScheduleResponse>(arguments, answer: null);

    public Task<GetFreeSpansResponse> GetFreeSpansAsync(
        GetFreeSpansArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<GetFreeSpansResponse>(arguments, answer: null);

    public Task<SearchFilesResponse> SearchFilesAsync(
        SearchFilesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        Answer<SearchFilesResponse>(arguments, answer: null);

    private Task<TResponse> Answer<TResponse>(object arguments, TResponse? answer)
        where TResponse : class
    {
        Calls.Add(arguments);
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(answer ?? throw new InvalidOperationException(message: "The read is not scripted."));
    }
}
