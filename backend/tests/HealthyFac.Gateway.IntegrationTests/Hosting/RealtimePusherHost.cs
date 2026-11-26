using HealthyFac.Shared.Infrastructure.Time;
using HealthyFac.Shared.Web.IdEncryption;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// A host of a service that pushes to the hub: it registers <c>AddSharedRealtime</c> and maps no hub, like a service
/// behind the gateway. The only way to the clients of the gateway is the Redis backplane.
/// </summary>
public sealed class RealtimePusherHost : IAsyncDisposable
{
    private const string TestEnvironment = "Test";
    private const string IdEncryptionKey = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    private readonly WebApplication _app;

    private RealtimePusherHost(WebApplication app)
    {
        _app = app;
    }

    public IHubContext<RealtimeHub, IRealtimeClient> Hub =>
        _app.Services.GetRequiredService<IHubContext<RealtimeHub, IRealtimeClient>>();

    public static async Task<RealtimePusherHost> StartAsync(string redisConnectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = TestEnvironment });
        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [GatewaySettingKeys.RedisConnectionString] = redisConnectionString,
                ["IdEncryption:Keys:0:Prefix"] = "v1_",
                ["IdEncryption:Keys:0:Key"] = IdEncryptionKey,
                ["IdEncryption:Keys:0:IsDefault"] = bool.TrueString,
            });

        builder.Services.AddSharedTime();
        builder.Services.AddIdEncryption(builder.Configuration);
        builder.Services.AddSharedRealtime(builder.Configuration);

        var app = builder.Build();
        await app.StartAsync();

        return new RealtimePusherHost(app);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
