using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

public static class GatewayLoggerFactory
{
    private const string LevelsSection = "Serilog:MinimumLevel";

    private static readonly TimeSpan LineTimeout = TimeSpan.FromSeconds(seconds: 10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(milliseconds: 100);

    /// <summary>
    /// Routes every log entry of the gateway host into <paramref name="logs"/> with the levels of the gateway
    /// configuration. <c>AddSerilog</c> gives each host category the static logger that exists when the category is
    /// created, so the host factory is replaced and <see cref="Log.Logger"/> is set to the same logger. The request
    /// log middleware reads the Serilog logger of the host, so the same logger is also its registration.
    /// </summary>
    public static void CaptureHostLogs(IServiceCollection services, LogTextCapture logs)
    {
        Logger? logger = null;

        Logger GetLogger(IServiceProvider provider)
        {
            return logger ??= Create(provider.GetRequiredService<IConfiguration>(), logs);
        }

        services.AddSingleton<Serilog.ILogger>(GetLogger);

        services.AddSingleton<ILoggerFactory>(provider =>
        {
            var hostLogger = GetLogger(provider);
            Log.Logger = hostLogger;

            return new SerilogLoggerFactory(hostLogger, dispose: true);
        });
    }

    /// <summary>
    /// Waits until a captured line matches <paramref name="isMatch"/> or the timeout ends. The request log line
    /// arrives after the response.
    /// </summary>
    public static async Task WaitForLineAsync(LogTextCapture logs, Func<string, bool> isMatch)
    {
        var deadline = DateTimeOffset.UtcNow + LineTimeout;

        while (DateTimeOffset.UtcNow < deadline && !logs.Lines.Any(isMatch))
        {
            await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Builds a logger that applies the minimum levels of the gateway configuration and writes to the sink only, so
    /// the test sees the log entries that the configured levels let through.
    /// </summary>
    public static Logger Create(IConfiguration gatewayConfiguration, ILogEventSink sink)
    {
        var levels = new ConfigurationBuilder()
            .AddInMemoryCollection(
                gatewayConfiguration.GetSection(LevelsSection)
                    .AsEnumerable(makePathsRelative: true)
                    .Where(pair => pair.Value is not null)
                    .Select(pair => new KeyValuePair<string, string?>($"{LevelsSection}:{pair.Key}", pair.Value)))
            .Build();

        return new LoggerConfiguration().ReadFrom.Configuration(levels).WriteTo.Sink(sink).CreateLogger();
    }
}
