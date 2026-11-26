using System.Net.Http.Json;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Infrastructure.HttpFailures;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Reads the open work orders of a machine from Core (<c>GET /v1/machines/{machineId}/open-work-orders</c>). The call
/// runs inside the downstream scope that the caller entered (<c>work-orders</c>). The route and the answer use
/// encrypted IDs, so the reader encrypts the machine ID and decrypts each work order ID.
/// </summary>
/// <param name="httpClientFactory">Creates the Core client.</param>
/// <param name="idEncryptor">Encrypts and decrypts the IDs of the API.</param>
public sealed class CoreWorkOrderCatalog(IHttpClientFactory httpClientFactory, IIdEncryptor idEncryptor)
    : IWorkOrderCatalog
{
    private const string UnavailableMessage = "Core did not answer the open work orders request.";
    private const string OpenWorkOrdersFormat = "v1/machines/{0}/open-work-orders?limit={1}";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IIdEncryptor _idEncryptor = idEncryptor;

    public async Task<IReadOnlyList<OpenWorkOrderInfo>> ListOpenAsync(
        Guid machineId,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var payload = await ReadPayloadAsync(machineId, limit, cancellationToken);

        return
        [
            .. payload
                .Select(item => new OpenWorkOrderInfo
                {
                    Id = _idEncryptor.Decrypt(item.Id),
                    Title = item.Title,
                    Status = item.Status,
                    Priority = item.Priority,
                    Category = item.Category,
                    PlannedStartUtc = item.PlannedStartUtc,
                    PlannedEndUtc = item.PlannedEndUtc,
                }),
        ];
    }

    /// <summary>
    /// Reads the answer of Core. An outage (no connection, a timeout, or a 5xx) ends in a
    /// <see cref="ProviderUnavailableException"/>, so a planner run fails as unavailable instead of going on without
    /// the data. Any other failure (a 4xx) stays an <see cref="HttpRequestException"/> for the model.
    /// </summary>
    private async Task<List<OpenWorkOrderPayload>> ReadPayloadAsync(
        Guid machineId,
        int limit,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(CoreClientNames.Client);

            using var response = await client.GetAsync(
                string.Format(OpenWorkOrdersFormat, _idEncryptor.Encrypt(machineId), limit),
                cancellationToken);

            await response.EnsureSuccessAsync(cancellationToken);

            return await response.Content.ReadFromJsonAsync<List<OpenWorkOrderPayload>>(cancellationToken) ?? [];
        }
        catch (HttpRequestException exception) when (IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }

    private static bool IsOutage(HttpRequestException exception) =>
        exception.StatusCode is null or >= System.Net.HttpStatusCode.InternalServerError;

    private sealed record OpenWorkOrderPayload
    {
        public required string Id { get; init; }

        public required string Title { get; init; }

        public required string Status { get; init; }

        public required string Priority { get; init; }

        public required string Category { get; init; }

        public DateTime? PlannedStartUtc { get; init; }

        public DateTime? PlannedEndUtc { get; init; }
    }
}
