using System.Net;
using System.Net.Http.Json;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Insights.Infrastructure.Extraction;

/// <summary>
/// Reads the active metric keys (<c>GET /v1/machines/{machineId}/metric-definitions</c>) and the status codes
/// (<c>POST /v1/machines/{machineId}/status-definitions/search</c>, every page) of a machine from Telemetry with the
/// service token. The calls run in the downstream scope <c>telemetry</c>. A <c>404</c> gives empty lists, because the
/// registration of the machine in Telemetry can still be missing.
/// </summary>
/// <param name="httpClientFactory">Creates the Telemetry client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the calls.</param>
/// <param name="idEncryptor">Encrypts the machine ID of the route.</param>
public sealed class TelemetryMachineSignalCatalog(
    IHttpClientFactory httpClientFactory,
    IDownstreamScopeHolder downstreamScopes,
    IIdEncryptor idEncryptor
) : IMachineSignalCatalog
{
    private const string MetricDefinitionsFormat = "v1/machines/{0}/metric-definitions";
    private const string StatusSearchFormat = "v1/machines/{0}/status-definitions/search";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;
    private readonly IIdEncryptor _idEncryptor = idEncryptor;

    public async Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(TelemetryClientNames.Client);
        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);
        var encryptedId = _idEncryptor.Encrypt(machineId);

        var metrics = await ReadMetricsAsync(client, encryptedId, cancellationToken);

        if (metrics is null)
        {
            return Empty();
        }

        var statuses = await ReadStatusesAsync(client, encryptedId, cancellationToken);

        return statuses is null ? Empty() : new MachineSignals { Metrics = metrics, Statuses = statuses };
    }

    private static MachineSignals Empty() => new() { Metrics = [], Statuses = [] };

    private async Task<List<SignalReference>?> ReadMetricsAsync(
        HttpClient client,
        string encryptedId,
        CancellationToken cancellationToken
    )
    {
        using var response = await client.GetAsync(
            string.Format(MetricDefinitionsFormat, encryptedId),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await response.EnsureSuccessAsync(cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<MetricDefinitionsPayload>(cancellationToken);

        return payload?.Definitions.Select(definition => ToReference(definition.Id, definition.Key)).ToList() ?? [];
    }

    private async Task<List<SignalReference>?> ReadStatusesAsync(
        HttpClient client,
        string encryptedId,
        CancellationToken cancellationToken
    )
    {
        var statuses = new List<SignalReference>();
        var offset = 0;

        while (true)
        {
            using var response = await client.PostAsJsonAsync(
                string.Format(StatusSearchFormat, encryptedId),
                new StatusSearchPayload { Offset = offset, Limit = GridValidationConstants.MaxLimit },
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await response.EnsureSuccessAsync(cancellationToken);
            var page = await response.Content.ReadFromJsonAsync<StatusPagePayload>(cancellationToken);

            if (page is null || page.Items.Count == 0)
            {
                return statuses;
            }

            statuses.AddRange(page.Items.Select(item => ToReference(item.Id, item.Code)));
            offset += page.Items.Count;

            if (offset >= page.TotalCount)
            {
                return statuses;
            }
        }
    }

    private SignalReference ToReference(string encryptedId, string key) =>
        new() { Id = _idEncryptor.Decrypt(encryptedId), Key = key };

    private sealed record MetricDefinitionsPayload
    {
        public required IReadOnlyList<KeyPayload> Definitions { get; init; }
    }

    private sealed record KeyPayload
    {
        public required string Id { get; init; }

        public required string Key { get; init; }
    }

    private sealed record StatusSearchPayload
    {
        public required int Offset { get; init; }

        public required int Limit { get; init; }

        public object Filter { get; init; } = new();
    }

    private sealed record StatusPagePayload
    {
        public required IReadOnlyList<CodePayload> Items { get; init; }

        public required int TotalCount { get; init; }
    }

    private sealed record CodePayload
    {
        public required string Id { get; init; }

        public required string Code { get; init; }
    }
}
