using HealthyFac.Insights.Application.Chats.Tools.Reads;

namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Reads the data of the user from Core, Telemetry, and Document for the chat tools. Every read runs with the
/// exchanged token of the user, so each service applies the permissions and the own scope of that user. Each method
/// takes the arguments of the model unchanged: no method takes a company or an actor.
/// </summary>
/// <remarks>
/// A target that does not exist, or that the user may not read, ends in a
/// <see cref="HealthyFac.Shared.Application.ErrorHandling.NotFoundException"/> with
/// <see cref="ChatToolErrors.NotFoundOrNotAllowed"/>, and never with the text of the reply.
/// </remarks>
public interface IChatReads
{
    Task<SearchMachinesResponse> SearchMachinesAsync(
        SearchMachinesArguments arguments,
        CancellationToken cancellationToken
    );

    Task<GetMachineResponse> GetMachineAsync(GetMachineArguments arguments, CancellationToken cancellationToken);

    Task<GetMachineStateTimesResponse> GetMachineStateTimesAsync(
        GetMachineStateTimesArguments arguments,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads the history of one metric.
    /// </summary>
    /// <param name="arguments">The arguments of the model.</param>
    /// <param name="bucketCount">The number of buckets, which the server owns.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The non-empty buckets in time order.</returns>
    Task<GetMachineMetricHistoryResponse> GetMetricHistoryAsync(
        GetMachineMetricHistoryArguments arguments,
        int bucketCount,
        CancellationToken cancellationToken
    );

    Task<GetMachineSignalEpisodesResponse> GetSignalEpisodesAsync(
        GetMachineSignalEpisodesArguments arguments,
        CancellationToken cancellationToken
    );

    Task<GetPhaseTimesResponse> GetPhaseTimesAsync(
        GetPhaseTimesArguments arguments,
        CancellationToken cancellationToken
    );

    Task<SearchWorkOrdersResponse> SearchWorkOrdersAsync(
        SearchWorkOrdersArguments arguments,
        CancellationToken cancellationToken
    );

    Task<GetWorkOrderResponse> GetWorkOrderAsync(
        GetWorkOrderArguments arguments,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads one active employee of the company by ID.
    /// </summary>
    /// <param name="employeeId">The employee to read.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The employee, or <see langword="null"/> when the company has no such active employee.</returns>
    Task<ChatEmployee?> FindEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);

    Task<SearchEmployeesResponse> SearchEmployeesAsync(
        SearchEmployeesArguments arguments,
        CancellationToken cancellationToken
    );

    Task<GetScheduleResponse> GetScheduleAsync(GetScheduleArguments arguments, CancellationToken cancellationToken);

    Task<GetFreeSpansResponse> GetFreeSpansAsync(
        GetFreeSpansArguments arguments,
        CancellationToken cancellationToken
    );

    Task<SearchFilesResponse> SearchFilesAsync(SearchFilesArguments arguments, CancellationToken cancellationToken);
}
