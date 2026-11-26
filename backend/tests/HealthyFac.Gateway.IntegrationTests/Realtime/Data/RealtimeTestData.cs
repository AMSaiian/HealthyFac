using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.TestHost;

namespace HealthyFac.Gateway.IntegrationTests.Realtime.Data;

public static class RealtimeTestData
{
    public static readonly Guid OtherCompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000099");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-0000000000aa");

    public static readonly IReadOnlyList<string> CasesReader = ["cases:read"];
    public static readonly IReadOnlyList<string> MachinesReader = ["machines:read"];

    public static TestTokenClaims Claims(
        Guid subject,
        IReadOnlyList<string>? permissions = null,
        Guid? companyId = null
    ) =>
        new()
        {
            Subject = subject,
            Organizations = [(companyId ?? TestTokenClaims.DefaultCompanyId).ToString()],
            Permissions = permissions ?? CasesReader,
        };

    public static RealtimeNotification CreateNotification(Guid actorSubject) =>
        new()
        {
            NotificationId = Guid.NewGuid(),
            Kind = RealtimeNotificationKind.WorkOrderAssigned,
            OccurredAt = DateTimeOffset.UtcNow,
            ActorSubject = actorSubject,
            Data = new TestNotificationData { MachineId = MachineId },
        };

    public static TheoryData<string, string?> AcceptedOrigins => new()
    {
        { "listed origin", GatewayFactory.DefaultCorsOrigin },
        { "no origin", null },
    };

    public static WebSocketClient CreateWebSocketClient(GatewayFactory gateway)
    {
        var client = gateway.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Origin = GatewayFactory.DefaultCorsOrigin;

        return client;
    }
}
