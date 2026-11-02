using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a Mailpit container for integration tests, with the image from <see cref="TestImages"/>. Mailpit accepts
/// every email on its SMTP port and lists the messages through its HTTP API.
/// </summary>
public sealed class MailpitTestServer : IAsyncDisposable
{
    private const ushort SmtpContainerPort = 1025;
    private const ushort ApiContainerPort = 8025;
    private const string ReadinessPath = "/livez";
    private const string MessagesPath = "/api/v1/messages";
    private const string MessagePathFormat = "/api/v1/message/{0}";

    private readonly IContainer _container = new ContainerBuilder(TestImages.Mailpit)
        .WithPortBinding(SmtpContainerPort, true)
        .WithPortBinding(ApiContainerPort, true)
        .WithWaitStrategy(
            Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(ApiContainerPort)
                    .ForPath(ReadinessPath)))
        .Build();

    private HttpClient _client = new();

    public string SmtpHost => _container.Hostname;

    public int SmtpPort => _container.GetMappedPublicPort(SmtpContainerPort);

    public async Task StartAsync()
    {
        await _container.StartAsync();

        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(ApiContainerPort)}"),
        };
    }

    public async Task<IReadOnlyList<MailpitMessage>> GetMessagesAsync()
    {
        var list = await _client.GetFromJsonAsync<MailpitMessageList>(MessagesPath);

        return list!.Messages
            .Select(message => new MailpitMessage(
                message.Id,
                message.To.Select(address => address.Address).ToList(),
                message.Subject))
            .ToList();
    }

    public async Task<string> GetMessageTextAsync(string id)
    {
        var message = await GetMessageDetailAsync(id);

        return message.Text;
    }

    public async Task<string> GetMessageHtmlAsync(string id)
    {
        var message = await GetMessageDetailAsync(id);

        return message.Html;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _container.DisposeAsync();
    }

    private async Task<MailpitMessageDetail> GetMessageDetailAsync(string id)
    {
        var message = await _client.GetFromJsonAsync<MailpitMessageDetail>(string.Format(MessagePathFormat, id));

        return message!;
    }

    private sealed record MailpitMessageList
    {
        [JsonPropertyName("messages")]
        public required IReadOnlyList<MailpitMessageSummary> Messages { get; init; }
    }

    private sealed record MailpitMessageSummary
    {
        [JsonPropertyName("ID")]
        public required string Id { get; init; }

        [JsonPropertyName("To")]
        public required IReadOnlyList<MailpitAddress> To { get; init; }

        [JsonPropertyName("Subject")]
        public required string Subject { get; init; }
    }

    private sealed record MailpitAddress
    {
        [JsonPropertyName("Address")]
        public required string Address { get; init; }
    }

    private sealed record MailpitMessageDetail
    {
        [JsonPropertyName("Text")]
        public required string Text { get; init; }

        [JsonPropertyName("HTML")]
        public required string Html { get; init; }
    }
}

/// <summary>
/// One email that Mailpit received.
/// </summary>
/// <param name="Id">The Mailpit message ID.</param>
/// <param name="To">The recipient addresses.</param>
/// <param name="Subject">The subject line.</param>
public sealed record MailpitMessage(string Id, IReadOnlyList<string> To, string Subject);
