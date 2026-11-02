using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Sets the remote address of every request before any gateway middleware runs, like a socket would.
/// </summary>
public sealed class ClientAddressStartupFilter(IPAddress clientAddress) : IStartupFilter
{
    private readonly IPAddress _clientAddress = clientAddress;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = _clientAddress;

                return nextMiddleware();
            });

            next(app);
        };
}
