using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Contracts.Schedules;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Files;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Telemetry.Contracts.Signals;

namespace HealthyFac.Insights.Infrastructure.Chats.Reads;

/// <summary>
/// Reads the data of the user from Core, Telemetry, and Document through their gRPC ports, with the user clients: each
/// call carries the exchanged token of the user and no actor header, so the target service applies the permissions and
/// the own scope of that user. A <c>NotFound</c> or <c>PermissionDenied</c> reply ends in a
/// <see cref="NotFoundException"/> with the fixed text <see cref="ChatToolErrors.NotFoundOrNotAllowed"/>: the reply
/// body never reaches the model. A call that the caller cancelled ends in an <see cref="OperationCanceledException"/>,
/// and an outage (<c>Unavailable</c> or <c>DeadlineExceeded</c>) ends in a <see cref="ProviderUnavailableException"/>.
/// Any other failure stays an <see cref="RpcException"/>, which the tool shows to the model as a fixed failure text.
/// </summary>
/// <param name="clientFactory">Creates the named user clients.</param>
public sealed class GrpcChatReads(GrpcClientFactory clientFactory) : IChatReads
{
    private const string UnavailableMessage = "A service did not answer the chat tool request.";

    private readonly GrpcClientFactory _clientFactory = clientFactory;

    public Task<SearchMachinesResponse> SearchMachinesAsync(
        SearchMachinesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var request = new SearchMachinesRequest
                {
                    Name = arguments.Name ?? string.Empty,
                    SerialNumber = arguments.SerialNumber ?? string.Empty,
                    Limit = arguments.Limit ?? GridValidationConstants.MaxLimit,
                    Offset = arguments.Offset ?? 0,
                };
                request.States.AddRange(arguments.States ?? []);
                var reply = await Machines().SearchMachinesAsync(request, cancellationToken: cancellationToken);

                return new SearchMachinesResponse
                {
                    TotalCount = reply.TotalCount,
                    Machines = reply.Machines.Select(ToMachine).ToList(),
                };
            },
            cancellationToken);

    public Task<GetMachineResponse> GetMachineAsync(
        GetMachineArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var machine = await Machines()
                    .GetMachineAsync(
                        new GetMachineRequest { MachineId = arguments.MachineId.ToString() },
                        cancellationToken: cancellationToken);
                var definitions = await Definitions()
                    .ListDefinitionsAsync(
                        new ListDefinitionsRequest { MachineId = arguments.MachineId.ToString() },
                        cancellationToken: cancellationToken);

                return new GetMachineResponse
                {
                    Machine = ToMachine(machine),
                    Metrics = definitions.Metrics.Select(ToMetric).ToList(),
                    Statuses = definitions.Statuses
                        .Select(status => new ChatStatusDefinition
                        {
                            Id = Guid.Parse(status.Id),
                            Code = status.Code,
                            Severity = status.Severity,
                        })
                        .ToList(),
                };
            },
            cancellationToken);

    public Task<GetMachineStateTimesResponse> GetMachineStateTimesAsync(
        GetMachineStateTimesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Machines()
                    .GetMachineStateTimesAsync(
                        new GetMachineStateTimesRequest
                        {
                            MachineId = arguments.MachineId.ToString(),
                            From = ToTimestamp(arguments.From),
                            To = ToTimestamp(arguments.To),
                        },
                        cancellationToken: cancellationToken);

                return new GetMachineStateTimesResponse
                {
                    States = reply.States
                        .Select(state => new ChatStateTime { State = state.State, Seconds = state.Seconds })
                        .ToList(),
                    BeforeCreationSeconds = reply.BeforeCreationSeconds,
                };
            },
            cancellationToken);

    public Task<GetMetricHistoryResponse> GetMetricHistoryAsync(
        GetMetricHistoryArguments arguments,
        int bucketCount,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Signals()
                    .GetMetricHistoryAsync(
                        new GetMetricHistoryRequest
                        {
                            MachineId = arguments.MachineId.ToString(),
                            MetricDefinitionId = arguments.MetricDefinitionId.ToString(),
                            From = ToTimestamp(arguments.From),
                            To = ToTimestamp(arguments.To),
                            BucketCount = bucketCount,
                        },
                        cancellationToken: cancellationToken);

                return new GetMetricHistoryResponse
                {
                    Buckets = reply.Buckets
                        .Select(bucket => new ChatMetricBucket
                        {
                            Start = bucket.Start.ToDateTime(),
                            Avg = bucket.Avg,
                            Min = bucket.Min,
                            Max = bucket.Max,
                            Count = bucket.Count,
                        })
                        .ToList(),
                };
            },
            cancellationToken);

    public Task<GetSignalEpisodesResponse> GetSignalEpisodesAsync(
        GetSignalEpisodesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Signals()
                    .GetSignalEpisodesAsync(
                        new GetSignalEpisodesRequest
                        {
                            MachineId = arguments.MachineId.ToString(),
                            From = ToTimestamp(arguments.From),
                            To = ToTimestamp(arguments.To),
                        },
                        cancellationToken: cancellationToken);

                return new GetSignalEpisodesResponse { Episodes = reply.Episodes.Select(ToEpisode).ToList() };
            },
            cancellationToken);

    public Task<GetPhaseTimesResponse> GetPhaseTimesAsync(
        GetPhaseTimesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Signals()
                    .GetPhaseTimesAsync(
                        new GetPhaseTimesRequest
                        {
                            MachineId = arguments.MachineId.ToString(),
                            From = ToTimestamp(arguments.From),
                            To = ToTimestamp(arguments.To),
                        },
                        cancellationToken: cancellationToken);

                return new GetPhaseTimesResponse
                {
                    Phases = reply.Phases
                        .Select(phase => new ChatPhaseTime
                        {
                            Code = phase.Code,
                            Name = phase.Name,
                            IsTracked = phase.IsTracked,
                            Seconds = phase.Seconds,
                        })
                        .ToList(),
                    UnknownModeSeconds = reply.UnknownModeSeconds,
                    NoDataSeconds = reply.NoDataSeconds,
                };
            },
            cancellationToken);

    public Task<SearchWorkOrdersResponse> SearchWorkOrdersAsync(
        SearchWorkOrdersArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var request = new SearchWorkOrdersRequest
                {
                    AssigneeId = arguments.AssigneeId?.ToString() ?? string.Empty,
                    MachineId = arguments.MachineId?.ToString() ?? string.Empty,
                    Limit = arguments.Limit ?? GridValidationConstants.MaxLimit,
                    Offset = arguments.Offset ?? 0,
                    SortColumn = arguments.SortColumn ?? string.Empty,
                    SortDirection = arguments.SortDirection ?? string.Empty,
                };
                request.Statuses.AddRange(arguments.Statuses ?? []);

                if (arguments.PlannedFrom is { } plannedFrom)
                {
                    request.PlannedFrom = ToTimestamp(plannedFrom);
                }

                if (arguments.PlannedTo is { } plannedTo)
                {
                    request.PlannedTo = ToTimestamp(plannedTo);
                }

                var reply = await WorkOrders().SearchWorkOrdersAsync(request, cancellationToken: cancellationToken);

                return new SearchWorkOrdersResponse
                {
                    TotalCount = reply.TotalCount,
                    WorkOrders = reply.WorkOrders.Select(ToWorkOrderRow).ToList(),
                };
            },
            cancellationToken);

    public Task<GetWorkOrderResponse> GetWorkOrderAsync(
        GetWorkOrderArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await WorkOrders()
                    .GetWorkOrderAsync(
                        new GetWorkOrderRequest
                        {
                            WorkOrderId = arguments.WorkOrderId.ToString(),
                            HistoryLimit = GridValidationConstants.MaxLimit,
                        },
                        cancellationToken: cancellationToken);

                return ToWorkOrder(reply);
            },
            cancellationToken);

    public Task<ChatEmployee?> FindEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
        CallAsync<ChatEmployee?>(
            async () =>
            {
                var reply = await Employees()
                    .LookupEmployeesAsync(
                        new LookupEmployeesRequest { EmployeeId = employeeId.ToString() },
                        cancellationToken: cancellationToken);
                var employee = reply.Employees.SingleOrDefault();

                return employee is null
                    ? null
                    : new ChatEmployee { Id = Guid.Parse(employee.Id), DisplayName = employee.DisplayName };
            },
            cancellationToken);

    public Task<SearchEmployeesResponse> SearchEmployeesAsync(
        SearchEmployeesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Employees()
                    .LookupEmployeesAsync(
                        new LookupEmployeesRequest { Search = arguments.Search ?? string.Empty },
                        cancellationToken: cancellationToken);

                return new SearchEmployeesResponse
                {
                    Employees = reply.Employees
                        .Select(employee => new ChatEmployee
                        {
                            Id = Guid.Parse(employee.Id),
                            DisplayName = employee.DisplayName,
                        })
                        .ToList(),
                };
            },
            cancellationToken);

    public Task<GetScheduleResponse> GetScheduleAsync(
        GetScheduleArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                if (arguments.EmployeeId is { } employeeId)
                {
                    var day = await Schedules()
                        .GetEmployeeScheduleAsync(
                            new GetEmployeeScheduleRequest
                            {
                                EmployeeId = employeeId.ToString(),
                                Date = arguments.Date,
                            },
                            cancellationToken: cancellationToken);

                    return new GetScheduleResponse
                    {
                        Date = day.Date,
                        TimeZoneId = day.TimeZoneId,
                        Days = [ToDay(day, employeeId, employeeName: null)],
                    };
                }

                var company = await Schedules()
                    .GetCompanyScheduleAsync(
                        new GetCompanyScheduleRequest { Date = arguments.Date },
                        cancellationToken: cancellationToken);

                return new GetScheduleResponse
                {
                    Date = company.Date,
                    TimeZoneId = company.TimeZoneId,
                    Days = company.Employees
                        .Select(employee => ToDay(
                            employee.Day,
                            Guid.Parse(employee.EmployeeId),
                            $"{employee.FirstName} {employee.LastName}"))
                        .ToList(),
                };
            },
            cancellationToken);

    public Task<GetFreeSpansResponse> GetFreeSpansAsync(
        GetFreeSpansArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await Schedules()
                    .GetFreeSpansAsync(
                        new GetFreeSpansRequest
                        {
                            EmployeeId = arguments.EmployeeId.ToString(),
                            Date = arguments.Date,
                        },
                        cancellationToken: cancellationToken);

                return new GetFreeSpansResponse
                {
                    Date = reply.Date,
                    TimeZoneId = reply.TimeZoneId,
                    Spans = reply.Spans
                        .Select(span => new ChatScheduleBlock
                            { StartLocal = span.StartLocal, EndLocal = span.EndLocal })
                        .ToList(),
                };
            },
            cancellationToken);

    public Task<SearchFilesResponse> SearchFilesAsync(
        SearchFilesArguments arguments,
        CancellationToken cancellationToken
    ) =>
        CallAsync(
            async () =>
            {
                var reply = await _clientFactory
                    .CreateClient<DocumentFiles.DocumentFilesClient>(DocumentClientNames.User)
                    .SearchFilesAsync(
                        new SearchFilesRequest { Search = arguments.Search ?? string.Empty },
                        cancellationToken: cancellationToken);

                return new SearchFilesResponse
                {
                    Files = reply.Files
                        .Select(file => new ChatFile
                        {
                            FileId = Guid.Parse(file.FileId),
                            FileName = file.FileName,
                            Extension = file.Extension,
                            ContentType = file.ContentType,
                            SizeBytes = file.SizeBytes,
                            MachineId = file.HasMachineId ? Guid.Parse(file.MachineId) : null,
                        })
                        .ToList(),
                };
            },
            cancellationToken);

    private static async Task<TResult> CallAsync<TResult>(Func<Task<TResult>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.NotFound or StatusCode.PermissionDenied)
        {
            throw new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);
        }
        catch (RpcException exception) when (CoreWorkOrderCatalog.IsCancelledByCaller(exception, cancellationToken))
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (RpcException exception) when (CoreWorkOrderCatalog.IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }

    private static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static ChatMachine ToMachine(MachineReply machine) =>
        new()
        {
            Id = Guid.Parse(machine.Id),
            Name = machine.Name,
            SerialNumber = machine.SerialNumber,
            State = machine.State,
            ExpectedGoLiveOn = machine.ExpectedGoLiveOn,
            Description = machine.HasDescription ? machine.Description : null,
        };

    private static ChatMetricDefinition ToMetric(MetricDefinition metric) =>
        new()
        {
            Id = Guid.Parse(metric.Id),
            Key = metric.Key,
            Name = metric.Name,
            ValueKind = metric.ValueKind,
            Unit = metric.Unit,
            Min = metric.HasMin ? metric.Min : null,
            Max = metric.HasMax ? metric.Max : null,
            States = metric.States.ToList(),
        };

    private static ChatSignalEpisode ToEpisode(SignalEpisode episode) =>
        new()
        {
            EpisodeId = Guid.Parse(episode.EpisodeId),
            Rule = episode.Rule,
            Severity = episode.Severity,
            State = episode.State,
            MetricKey = episode.HasMetricKey ? episode.MetricKey : null,
            StatusCode = episode.HasStatusCode ? episode.StatusCode : null,
            PhaseCode = episode.HasPhaseCode ? episode.PhaseCode : null,
            Value = episode.HasValue ? episode.Value : null,
            Limit = episode.HasLimit ? episode.Limit : null,
            Z = episode.HasZ ? episode.Z : null,
            ForecastSeconds = episode.HasForecastSeconds ? episode.ForecastSeconds : null,
            OpenedAt = episode.OpenedAt.ToDateTime(),
            ResolvedAt = episode.ResolvedAt?.ToDateTime(),
        };

    private static ChatWorkOrderRow ToWorkOrderRow(WorkOrderRow row) =>
        new()
        {
            Id = Guid.Parse(row.Id),
            Title = row.Title,
            Status = row.Status,
            Priority = row.Priority,
            Category = row.Category,
            MachineId = row.HasMachineId ? Guid.Parse(row.MachineId) : null,
            AssigneeId = row.HasAssigneeId ? Guid.Parse(row.AssigneeId) : null,
            AssigneeName = row.HasAssigneeName ? row.AssigneeName : null,
            PlannedStart = row.PlannedStart?.ToDateTime(),
            PlannedEnd = row.PlannedEnd?.ToDateTime(),
            EstimateMinutes = row.EstimateMinutes,
        };

    private static GetWorkOrderResponse ToWorkOrder(WorkOrderReply reply) =>
        new()
        {
            Id = Guid.Parse(reply.Id),
            Title = reply.Title,
            Description = reply.HasDescription ? reply.Description : null,
            Status = reply.Status,
            Priority = reply.Priority,
            Category = reply.Category,
            MachineId = reply.HasMachineId ? Guid.Parse(reply.MachineId) : null,
            MachineName = reply.HasMachineName ? reply.MachineName : null,
            AssigneeId = reply.HasAssigneeId ? Guid.Parse(reply.AssigneeId) : null,
            AssigneeName = reply.HasAssigneeName ? reply.AssigneeName : null,
            PlannedStart = reply.PlannedStart?.ToDateTime(),
            PlannedEnd = reply.PlannedEnd?.ToDateTime(),
            EstimateMinutes = reply.EstimateMinutes,
            IsDelayed = reply.IsDelayed,
            CreatedAt = reply.CreatedAt.ToDateTime(),
            History = reply.History
                .Select(row => new ChatWorkOrderHistoryRow
                {
                    Revision = row.Revision,
                    Status = row.Status,
                    AssigneeId = row.HasAssigneeId ? Guid.Parse(row.AssigneeId) : null,
                    AssigneeName = row.HasAssigneeName ? row.AssigneeName : null,
                    PlannedStart = row.PlannedStart?.ToDateTime(),
                    EstimateMinutes = row.EstimateMinutes,
                    CreatedAt = row.CreatedAt.ToDateTime(),
                })
                .ToList(),
        };

    private static ChatScheduleDay ToDay(EmployeeScheduleReply day, Guid employeeId, string? employeeName) =>
        new()
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Source = day.Source,
            Blocks = day.Blocks
                .Select(block => new ChatScheduleBlock { StartLocal = block.StartLocal, EndLocal = block.EndLocal })
                .ToList(),
            WorkOrders = day.WorkOrders
                .Select(workOrder => new ChatScheduledWorkOrder
                {
                    Id = Guid.Parse(workOrder.Id),
                    Title = workOrder.Title,
                    Status = workOrder.Status,
                    Priority = workOrder.Priority,
                    PlannedStartLocal = workOrder.PlannedStartLocal,
                    EstimateMinutes = workOrder.EstimateMinutes,
                    IsDelayed = workOrder.IsDelayed,
                })
                .ToList(),
        };

    private CoreMachines.CoreMachinesClient Machines() =>
        _clientFactory.CreateClient<CoreMachines.CoreMachinesClient>(CoreClientNames.User.Machines);

    private CoreWorkOrders.CoreWorkOrdersClient WorkOrders() =>
        _clientFactory.CreateClient<CoreWorkOrders.CoreWorkOrdersClient>(CoreClientNames.User.WorkOrders);

    private CoreEmployees.CoreEmployeesClient Employees() =>
        _clientFactory.CreateClient<CoreEmployees.CoreEmployeesClient>(CoreClientNames.User.Employees);

    private CoreSchedules.CoreSchedulesClient Schedules() =>
        _clientFactory.CreateClient<CoreSchedules.CoreSchedulesClient>(CoreClientNames.User.Schedules);

    private TelemetrySignals.TelemetrySignalsClient Signals() =>
        _clientFactory.CreateClient<TelemetrySignals.TelemetrySignalsClient>(TelemetryClientNames.User.Signals);

    private TelemetryDefinitions.TelemetryDefinitionsClient Definitions() =>
        _clientFactory.CreateClient<TelemetryDefinitions.TelemetryDefinitionsClient>(
            TelemetryClientNames.User.Definitions);
}
