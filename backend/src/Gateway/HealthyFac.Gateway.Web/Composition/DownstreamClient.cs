using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Calls a service of the Gateway for a composed response. It uses the YARP cluster of the service, so the address,
/// the health state, and the timeout come from the same configuration as the proxy routes.
/// </summary>
/// <remarks>
/// The call carries only the <c>Authorization</c> header of the incoming request, so the service applies the
/// permissions and the company of the person. It never sends the actor headers, which only a service token may
/// carry. The call does not retry, because the person decides to try again. The client never logs a body.
/// </remarks>
/// <param name="proxyStateLookup">Finds the cluster and its available destinations.</param>
/// <param name="logger">Logs the failure of a call, without a body.</param>
public sealed class DownstreamClient(IProxyStateLookup proxyStateLookup, ILogger<DownstreamClient> logger)
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(seconds: 100);

    private readonly IProxyStateLookup _proxyStateLookup = proxyStateLookup;
    private readonly ILogger<DownstreamClient> _logger = logger;

    /// <summary>
    /// Sends one request to the first available destination of a cluster.
    /// </summary>
    /// <param name="clusterId">The YARP cluster ID, from <see cref="ComposedClusters"/>.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path with the query, starting with <c>/</c>, without the public prefix.</param>
    /// <param name="body">The JSON body, or <see langword="null"/> for none.</param>
    /// <param name="incoming">The request of the person. Only its <c>Authorization</c> header is copied.</param>
    /// <param name="cancellationToken">Cancels the call, for example when the person leaves.</param>
    /// <returns>The result of the call. A failure of the service or the network is
    /// <see cref="DownstreamResult.Unavailable"/>, never an exception.</returns>
    public async Task<DownstreamResult> SendAsync(
        string clusterId,
        HttpMethod method,
        string path,
        JsonNode? body,
        HttpContext incoming,
        CancellationToken cancellationToken
    )
    {
        if (!_proxyStateLookup.TryGetCluster(clusterId, out var cluster))
        {
            return new DownstreamResult.Unavailable();
        }

        var destination = cluster.DestinationsState.AvailableDestinations.FirstOrDefault();

        if (destination is null)
        {
            return new DownstreamResult.Unavailable();
        }

        using var request = new HttpRequestMessage(
            method, destination.Model.Config.Address.TrimEnd(trimChar: '/') + path);

        if (incoming.Request.Headers.Authorization.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                HeaderNames.Authorization,
                incoming.Request.Headers.Authorization.ToString());
        }

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(cluster.Model.Config.HttpRequest?.ActivityTimeout ?? DefaultTimeout);

        try
        {
            using var response = await cluster.Model.HttpClient.SendAsync(request, timeout.Token);

            return await ReadResultAsync(clusterId, response, timeout.Token);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, message: "The call to cluster {ClusterId} failed to connect.", clusterId);

            return new DownstreamResult.Unavailable();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, message: "The call to cluster {ClusterId} timed out.", clusterId);

            return new DownstreamResult.Unavailable();
        }
    }

    private async Task<DownstreamResult> ReadResultAsync(
        string clusterId,
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var status = (int)response.StatusCode;

        if (status >= (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogWarning(message: "Cluster {ClusterId} answered status {Status}.", clusterId, status);

            return new DownstreamResult.Unavailable();
        }

        var isClientError = status >= (int)HttpStatusCode.BadRequest;

        if (!response.IsSuccessStatusCode && !isClientError)
        {
            return new DownstreamResult.Unavailable();
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = TryParse(text);

        if (json is null)
        {
            _logger.LogWarning(
                message: "Cluster {ClusterId} answered status {Status} with a body that is not JSON.",
                clusterId,
                status);
        }

        return isClientError
            ? new DownstreamResult.ClientError { Status = status, Problem = json ?? new JsonObject() }
            : json is null
                ? new DownstreamResult.Unavailable()
                : new DownstreamResult.Success { Body = json };
    }

    private static JsonNode? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
