using HealthyFac.Auth.IntegrationTests.Keycloak.Data;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Auth.IntegrationTests.Identities.Data;

public static class ProvisioningTestData
{
    public const string QueueName = "auth-integration-tests";
    public const string DatabaseConnectionStringKey = "ConnectionStrings:Database";
    public const string BrokerConnectionStringKey = "ConnectionStrings:RabbitMq";
    public const string QueueNameKey = "Messaging:QueueName";
    public const string RedisConnectionStringKey = "Redis:ConnectionString";
    public const string PollingIntervalKey = "Messaging:PollingIntervalMilliseconds";
    public const string ServiceNameKey = "Service:Name";
    public const string PollingIntervalMilliseconds = "50";
    public const string OutboxProcessorTypeName = "OutboxProcessor";
    public const string EmployeeIdAttribute = "employee_id";
    public const string FirstName = "Ada";
    public const string LastName = "Worker";
    public const string KeycloakUnavailableMessage = "The Keycloak server is not reachable.";
    public const string RedisUnavailableMessage = "The Redis server is not reachable.";

    public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(seconds: 30);
    public static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(milliseconds: 100);
    public const int DelayedDeactivationMinutes = 10;

    public static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(seconds: 3);
    public static readonly TimeSpan ExpectedRevocationTimeToLive = TimeSpan.FromSeconds(seconds: 360);
    public static readonly TimeSpan MinimumRevocationTimeToLive = TimeSpan.FromSeconds(seconds: 300);

    public static readonly Guid ActorId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000a1");

    public static EmployeeAccepted CreateAccepted(Guid? companyId = null, string roleCode = "worker")
    {
        var employeeId = Guid.CreateVersion7();

        return new EmployeeAccepted
        {
            EmployeeId = employeeId,
            CompanyId = companyId ?? KeycloakAdminTestData.CompanyId,
            Email = $"{employeeId:N}@{KeycloakAdminTestData.EmailDomain}",
            FirstName = FirstName,
            LastName = LastName,
            RoleCode = roleCode,
        };
    }

    public static Dictionary<string, string> ActorHeaders(Guid companyId) =>
        new()
        {
            [ActorHeaderNames.ActorId] = ActorId.ToString(),
            [ActorHeaderNames.CompanyId] = companyId.ToString(),
        };

    public static EmployeeDeactivated CreateDeactivated(Guid? identityUserId, Guid? companyId = null) => new()
    {
        EmployeeId = Guid.CreateVersion7(),
        CompanyId = companyId ?? KeycloakAdminTestData.CompanyId,
        IdentityUserId = identityUserId,
        DeactivatedAt = DateTime.UtcNow,
    };

    public static TheoryData<EmployeeDeactivated> ValidDeactivatedMessages => new()
    {
        CreateDeactivated(Guid.CreateVersion7()),
        CreateDeactivated(identityUserId: null),
    };

    public static TheoryData<EmployeeDeactivated, string> InvalidDeactivatedMessagesWithExpectedError => new()
    {
        {
            CreateDeactivated(Guid.Empty),
            EmployeeMessageErrors.Validation.IdentityUserIdInvalid
        },
    };
}
