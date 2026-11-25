using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Contracts.Schedules;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Telemetry.Contracts.Signals;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public static class ChatClientRegistrationTestData
{
    private const string CoreAudience = "healthyfac-core";
    private const string CoreScope = "insights-chat-core";
    private const string TelemetryAudience = "healthyfac-telemetry";
    private const string TelemetryScope = "insights-chat-telemetry";

    public static TheoryData<string, Func<GrpcClientFactory, Task>, string, string> UserClients => new()
    {
        {
            "Machines",
            factory => factory.CreateClient<CoreMachines.CoreMachinesClient>(CoreClientNames.User.Machines)
                .SearchMachinesAsync(new SearchMachinesRequest())
                .ResponseAsync,
            CoreAudience,
            CoreScope
        },
        {
            "WorkOrders",
            factory => factory.CreateClient<CoreWorkOrders.CoreWorkOrdersClient>(CoreClientNames.User.WorkOrders)
                .SearchWorkOrdersAsync(new SearchWorkOrdersRequest())
                .ResponseAsync,
            CoreAudience,
            CoreScope
        },
        {
            "Employees",
            factory => factory.CreateClient<CoreEmployees.CoreEmployeesClient>(CoreClientNames.User.Employees)
                .LookupEmployeesAsync(new LookupEmployeesRequest())
                .ResponseAsync,
            CoreAudience,
            CoreScope
        },
        {
            "Schedules",
            factory => factory.CreateClient<CoreSchedules.CoreSchedulesClient>(CoreClientNames.User.Schedules)
                .GetFreeSpansAsync(new GetFreeSpansRequest())
                .ResponseAsync,
            CoreAudience,
            CoreScope
        },
        {
            "Signals",
            factory => factory.CreateClient<TelemetrySignals.TelemetrySignalsClient>(TelemetryClientNames.User.Signals)
                .GetPhaseTimesAsync(new GetPhaseTimesRequest())
                .ResponseAsync,
            TelemetryAudience,
            TelemetryScope
        },
        {
            "Definitions",
            factory => factory
                .CreateClient<TelemetryDefinitions.TelemetryDefinitionsClient>(TelemetryClientNames.User.Definitions)
                .ListDefinitionsAsync(new ListDefinitionsRequest())
                .ResponseAsync,
            TelemetryAudience,
            TelemetryScope
        },
    };
}
