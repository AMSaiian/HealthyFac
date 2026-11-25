using System.Net;
using System.Net.Sockets;
using Grpc.Core;
using Grpc.Net.Client;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Contracts.Schedules;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.ApiTests.Hosting;

/// <summary>
/// Runs the Core host on real Kestrel ports: the API port with HTTP/1.1 and the gRPC port with HTTP/2 without TLS. The
/// in-memory test server has no ports, so it cannot serve gRPC.
/// </summary>
public sealed class CoreGrpcHost : IAsyncDisposable
{
    private readonly CoreApiFactory _factory;
    private readonly GrpcChannel _channel;

    public CoreGrpcHost(IReadOnlyDictionary<string, string?> settings, TestTokenFactory tokens)
    {
        var apiPort = FreePort();
        var grpcPort = FreePort();
        var hostSettings = new Dictionary<string, string?>(settings) { [key: "Grpc:Port"] = grpcPort.ToString() };

        _factory = new CoreApiFactory(
            hostSettings,
            tokens,
            services => services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Loopback, apiPort);
                options.Listen(IPAddress.Loopback, grpcPort);
            }));
        _factory.UseKestrel();
        _factory.StartServer();

        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{grpcPort}");
        Machines = new CoreMachines.CoreMachinesClient(_channel);
        WorkOrders = new CoreWorkOrders.CoreWorkOrdersClient(_channel);
        Employees = new CoreEmployees.CoreEmployeesClient(_channel);
        Schedules = new CoreSchedules.CoreSchedulesClient(_channel);
    }

    public CoreMachines.CoreMachinesClient Machines { get; }

    public CoreWorkOrders.CoreWorkOrdersClient WorkOrders { get; }

    public CoreEmployees.CoreEmployeesClient Employees { get; }

    public CoreSchedules.CoreSchedulesClient Schedules { get; }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _factory.DisposeAsync();
    }

    public static Metadata BearerHeaders(string token, Guid? actorId = null, Guid? companyId = null)
    {
        var headers = new Metadata { { "Authorization", $"Bearer {token}" } };

        if (actorId is not null && companyId is not null)
        {
            headers.Add(ActorHeaderNames.ActorId, actorId.Value.ToString());
            headers.Add(ActorHeaderNames.CompanyId, companyId.Value.ToString());
        }

        return headers;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
