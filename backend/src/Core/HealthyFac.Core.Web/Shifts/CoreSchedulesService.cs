using Grpc.Core;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Contracts.Schedules;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Authorization;
using ApplicationGetCompanyDayScheduleRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetCompanyDayScheduleRequest;
using ApplicationGetEmployeeDayScheduleRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetEmployeeDayScheduleRequest;
using ApplicationGetEmployeeFreeSpansRequest =
    HealthyFac.Core.Application.Shifts.DaySchedules.GetEmployeeFreeSpansRequest;
using RpcGetCompanyScheduleRequest = HealthyFac.Core.Contracts.Schedules.GetCompanyScheduleRequest;
using RpcGetEmployeeScheduleRequest = HealthyFac.Core.Contracts.Schedules.GetEmployeeScheduleRequest;
using RpcGetFreeSpansRequest = HealthyFac.Core.Contracts.Schedules.GetFreeSpansRequest;

namespace HealthyFac.Core.Web.Shifts;

/// <summary>
/// Serves the shift schedules to the services that read them (Insights) on the gRPC port. Each method runs the use case
/// of its HTTP endpoint with its permissions. The employee reads use the access scope of the token: a token without
/// `shifts:read-company` reaches only its own employee, and another employee is "not found".
/// </summary>
/// <param name="employeeHandler">Reads the day of one employee.</param>
/// <param name="companyHandler">Reads the day of the company.</param>
/// <param name="freeSpansHandler">Reads the free spans of one employee.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class CoreSchedulesService(
    IRequestHandler<ApplicationGetEmployeeDayScheduleRequest, DayScheduleResponse> employeeHandler,
    IRequestHandler<ApplicationGetCompanyDayScheduleRequest, CompanyDayScheduleResponse> companyHandler,
    IRequestHandler<ApplicationGetEmployeeFreeSpansRequest, FreeSpansResponse> freeSpansHandler,
    ICurrentUserProvider currentUser
) : CoreSchedules.CoreSchedulesBase
{
    private readonly IRequestHandler<ApplicationGetEmployeeDayScheduleRequest, DayScheduleResponse>
        _employeeHandler = employeeHandler;

    private readonly IRequestHandler<ApplicationGetCompanyDayScheduleRequest, CompanyDayScheduleResponse>
        _companyHandler = companyHandler;

    private readonly IRequestHandler<ApplicationGetEmployeeFreeSpansRequest, FreeSpansResponse> _freeSpansHandler =
        freeSpansHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    [Authorize(Policy = CorePermissions.Shifts.Read)]
    public override async Task<EmployeeScheduleReply> GetEmployeeSchedule(
        RpcGetEmployeeScheduleRequest request,
        ServerCallContext context
    )
    {
        var employeeId = RpcInput.ParseId(request.EmployeeId);
        var date = RpcInput.ParseDate(request.Date);

        var response = await _employeeHandler.HandleAsync(
            new ApplicationGetEmployeeDayScheduleRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Scope = AccessScopes.Resolve(context.GetHttpContext().User, CorePermissions.Shifts.ReadCompany),
                EmployeeId = employeeId,
                Date = date,
            },
            context.CancellationToken);

        return ToReply(response);
    }

    [Authorize(Policy = CorePermissions.Shifts.ReadCompany)]
    [Authorize(Policy = CorePermissions.Employees.Read)]
    public override async Task<CompanyScheduleReply> GetCompanySchedule(
        RpcGetCompanyScheduleRequest request,
        ServerCallContext context
    )
    {
        var date = RpcInput.ParseDate(request.Date);

        var response = await _companyHandler.HandleAsync(
            new ApplicationGetCompanyDayScheduleRequest { CompanyId = _currentUser.GetCompanyId(), Date = date },
            context.CancellationToken);

        var reply = new CompanyScheduleReply
        {
            Date = RpcInput.ToDateText(response.Date),
            TimeZoneId = response.TimeZoneId,
        };

        reply.Employees.AddRange(
            response.Employees.Select(employee => new CompanyScheduleEmployee
            {
                EmployeeId = employee.EmployeeId.ToString(),
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Day = ToReply(employee.Day),
            }));

        return reply;
    }

    [Authorize(Policy = CorePermissions.Shifts.Read)]
    public override async Task<FreeSpansReply> GetFreeSpans(RpcGetFreeSpansRequest request, ServerCallContext context)
    {
        var employeeId = RpcInput.ParseId(request.EmployeeId);
        var date = RpcInput.ParseDate(request.Date);

        var response = await _freeSpansHandler.HandleAsync(
            new ApplicationGetEmployeeFreeSpansRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                Scope = AccessScopes.Resolve(context.GetHttpContext().User, CorePermissions.Shifts.ReadCompany),
                EmployeeId = employeeId,
                Date = date,
            },
            context.CancellationToken);

        var reply = new FreeSpansReply { Date = RpcInput.ToDateText(response.Date), TimeZoneId = response.TimeZoneId };

        reply.Spans.AddRange(
            response.Spans.Select(span => new FreeSpan
            {
                StartLocal = RpcInput.ToLocalDateTimeText(span.StartLocal),
                EndLocal = RpcInput.ToLocalDateTimeText(span.EndLocal),
                Start = RpcTimestamps.ToTimestamp(span.StartUtc),
                End = RpcTimestamps.ToTimestamp(span.EndUtc),
            }));

        return reply;
    }

    private static EmployeeScheduleReply ToReply(DayScheduleResponse day)
    {
        var reply = new EmployeeScheduleReply
        {
            Date = RpcInput.ToDateText(day.Date),
            TimeZoneId = day.TimeZoneId,
            Source = day.Source.ToString(),
        };

        reply.Blocks.AddRange(
            day.Blocks.Select(block => new ScheduleBlock
            {
                StartLocal = RpcInput.ToLocalTimeText(block.StartLocal),
                EndLocal = RpcInput.ToLocalTimeText(block.EndLocal),
                Start = RpcTimestamps.ToTimestamp(block.StartUtc),
                End = RpcTimestamps.ToTimestamp(block.EndUtc),
            }));

        reply.WorkOrders.AddRange(
            day.WorkOrders.Select(workOrder => new ScheduledWorkOrder
            {
                Id = workOrder.Id.ToString(),
                Title = workOrder.Title,
                Status = workOrder.Status.ToString(),
                Priority = workOrder.Priority.ToString(),
                PlannedStartLocal = RpcInput.ToLocalDateTimeText(workOrder.PlannedStartLocal),
                PlannedStart = RpcTimestamps.ToTimestamp(workOrder.PlannedStartUtc),
                EstimateMinutes = workOrder.EstimateMinutes,
                OccupancyEnd = RpcTimestamps.ToTimestamp(workOrder.OccupancyEndUtc),
                IsDelayed = workOrder.IsDelayed,
            }));

        return reply;
    }
}
