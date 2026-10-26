using System.Text;
using RabbitMQ.Client;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// Reads queues of the test broker directly, so a test sees what Rebus moved or left there.
/// </summary>
public static class RabbitMqQueueReader
{
    public static async Task<int> CountReadyMessagesAsync(string connectionString, string queueName)
    {
        await using var connection = await ConnectAsync(connectionString);
        await using var channel = await connection.CreateChannelAsync();

        var queue = await channel.QueueDeclarePassiveAsync(queueName);

        return (int)queue.MessageCount;
    }

    /// <summary>
    /// Takes messages from <paramref name="queueName"/> until one carries the Rebus message ID
    /// <paramref name="messageId"/> or the timeout ends.
    /// </summary>
    /// <returns><see langword="true"/> when the queue held a message with that ID.</returns>
    public static async Task<bool> ContainsMessageAsync(
        string connectionString,
        string queueName,
        Guid messageId,
        TimeSpan timeout
    )
    {
        await using var connection = await ConnectAsync(connectionString);
        await using var channel = await connection.CreateChannelAsync();

        using var cancellation = new CancellationTokenSource(timeout);
        while (!cancellation.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken: cancellation.Token);
            if (result is null)
            {
                await Task.Delay(TransportTestData.PollDelay, cancellation.Token);

                continue;
            }

            if (ReadMessageId(result.BasicProperties) == messageId.ToString())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Takes messages from <paramref name="queueName"/> until one carries the Rebus message ID
    /// <paramref name="messageId"/> or the timeout ends, and reads one header of that message.
    /// </summary>
    /// <returns>The header text, or <see langword="null"/> when no message with that ID arrived.</returns>
    public static async Task<string?> ReadHeaderAsync(
        string connectionString,
        string queueName,
        Guid messageId,
        string headerName,
        TimeSpan timeout
    )
    {
        await using var connection = await ConnectAsync(connectionString);
        await using var channel = await connection.CreateChannelAsync();

        using var cancellation = new CancellationTokenSource(timeout);
        while (!cancellation.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result is null)
            {
                await Task.Delay(TransportTestData.PollDelay);

                continue;
            }

            if (ReadMessageId(result.BasicProperties) == messageId.ToString())
            {
                return ReadHeader(result.BasicProperties, headerName);
            }
        }

        return null;
    }

    private static string? ReadHeader(IReadOnlyBasicProperties properties, string headerName)
        => properties.Headers is not null
            && properties.Headers.TryGetValue(headerName, out var value)
            && value is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : null;

    private static string? ReadMessageId(IReadOnlyBasicProperties properties)
        => properties.Headers is not null
            && properties.Headers.TryGetValue(TransportTestData.MessageIdHeader, out var value)
            && value is byte[] bytes
                ? Encoding.UTF8.GetString(bytes)
                : null;

    private static Task<IConnection> ConnectAsync(string connectionString)
        => new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnectionAsync();
}
