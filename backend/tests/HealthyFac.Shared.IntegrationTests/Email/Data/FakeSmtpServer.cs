using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HealthyFac.Shared.IntegrationTests.Email.Data;

/// <summary>
/// A minimal SMTP server on the loopback address for failure tests. It reaches cases that Mailpit cannot reach: a
/// server that never answers, and a server that resets the connection when the client says goodbye.
/// </summary>
public sealed class FakeSmtpServer : IAsyncDisposable
{
    private const string Greeting = "220 fake.test ready";
    private const string Accepted = "250 OK";
    private const string StartData = "354 End data with <CR><LF>.<CR><LF>";
    private const string DataEnd = ".";
    private const string QuitCommand = "QUIT";
    private const string DataCommand = "DATA";

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly FakeSmtpBehavior _behavior;
    private readonly Task _acceptLoop;

    public FakeSmtpServer(FakeSmtpBehavior behavior)
    {
        _behavior = behavior;
        _listener.Start();
        _acceptLoop = AcceptAsync(_stop.Token);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// Finds a loopback port where nothing listens, for a test of a refused connection.
    /// </summary>
    /// <returns>A port number that was free a moment ago.</returns>
    public static int FindClosedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _acceptLoop;
        _stop.Dispose();
    }

    private async Task AcceptAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = ServeAsync(client, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var connection = client;

        try
        {
            if (_behavior == FakeSmtpBehavior.Silent)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);

                return;
            }

            await TalkAsync(connection, cancellationToken);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException)
        {
        }
    }

    private static async Task TalkAsync(TcpClient connection, CancellationToken cancellationToken)
    {
        await using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

        await writer.WriteLineAsync(Greeting.AsMemory(), cancellationToken);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(QuitCommand, StringComparison.OrdinalIgnoreCase))
            {
                connection.LingerState = new LingerOption(true, 0);

                return;
            }

            if (line.StartsWith(DataCommand, StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync(StartData.AsMemory(), cancellationToken);
                await SkipDataAsync(reader, cancellationToken);
            }

            await writer.WriteLineAsync(Accepted.AsMemory(), cancellationToken);
        }
    }

    private static async Task SkipDataAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line && line != DataEnd)
        {
        }
    }
}

public enum FakeSmtpBehavior
{
    Silent,
    ResetOnQuit,
}
