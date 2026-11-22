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
    /// <summary>
    /// The name under which a container on the same network reaches Mailpit: the <c>mailpit</c> service of
    /// <c>docker-compose.yml</c>, which the SMTP server of the realm file names.
    /// </summary>
    private const string NetworkAlias = "mailpit";

    private const ushort SmtpContainerPort = 1025;

    private const ushort ApiContainerPort = 8025;
    private const string ReadinessPath = "/livez";
    private const string MessagesPath = "/api/v1/messages";
    private const string MessagePathFormat = "/api/v1/message/{0}";

    private readonly IContainer _container;

    private HttpClient _client = new();

    public MailpitTestServer()
    {
        _container = CreateBuilder().Build();
    }

    /// <summary>
    /// Creates a server that joins the given network under <see cref="NetworkAlias"/>. A container on that network
    /// sends email to <see cref="NetworkAlias"/> on <see cref="SmtpContainerPort"/>. The path through the Docker host
    /// and a published port loses connections under load.
    /// </summary>
    /// <param name="network">
    /// The network to join. The caller creates it before <see cref="StartAsync"/> and disposes it after this server.
    /// </param>
    public MailpitTestServer(TestNetwork network)
    {
        _container = CreateBuilder().WithNetwork(network.Network).WithNetworkAliases(NetworkAlias).Build();
    }

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

    /// <summary>Deletes every stored message, so a test counts only the emails of its own run.</summary>
    public async Task DeleteMessagesAsync()
    {
        using var response = await _client.DeleteAsync(MessagesPath);
        response.EnsureSuccessStatusCode();
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

    private static ContainerBuilder CreateBuilder() =>
        new ContainerBuilder(TestImages.Mailpit)
            .WithPortBinding(SmtpContainerPort, assignRandomHostPort: true)
            .WithPortBinding(ApiContainerPort, assignRandomHostPort: true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request
                        .ForPort(ApiContainerPort)
                        .ForPath(ReadinessPath)));

    private async Task<MailpitMessageDetail> GetMessageDetailAsync(string id)
    {
        var message = await _client.GetFromJsonAsync<MailpitMessageDetail>(string.Format(MessagePathFormat, id));

        return message!;
    }

    private sealed record MailpitMessageList
    {
        [JsonPropertyName(name: "messages")]
        public required IReadOnlyList<MailpitMessageSummary> Messages { get; init; }
    }

    private sealed record MailpitMessageSummary
    {
        [JsonPropertyName(name: "ID")]
        public required string Id { get; init; }

        [JsonPropertyName(name: "To")]
        public required IReadOnlyList<MailpitAddress> To { get; init; }

        [JsonPropertyName(name: "Subject")]
        public required string Subject { get; init; }
    }

    private sealed record MailpitAddress
    {
        [JsonPropertyName(name: "Address")]
        public required string Address { get; init; }
    }

    private sealed record MailpitMessageDetail
    {
        [JsonPropertyName(name: "Text")]
        public required string Text { get; init; }

        [JsonPropertyName(name: "HTML")]
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
