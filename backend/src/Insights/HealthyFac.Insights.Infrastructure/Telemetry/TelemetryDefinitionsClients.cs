using Grpc.Net.ClientFactory;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Picks the gRPC client of the Telemetry definitions for the current call. A request with the token of a user uses
/// the client with the exchanged token of the user, and every other call (a background flow) uses the client with the
/// service token and the actor headers. The service client throws for a request with a user token, so the pick is the
/// one place that decides.
/// </summary>
/// <param name="clientFactory">Creates the named clients.</param>
/// <param name="accessor">
/// Tells whether the current request has the token of a user. A host without HTTP requests (the seeder) registers
/// none, so every call uses the service client.
/// </param>
public sealed class TelemetryDefinitionsClients(
    GrpcClientFactory clientFactory,
    IIncomingAccessTokenAccessor? accessor = null
) : ITelemetryDefinitionsClients
{
    private readonly GrpcClientFactory _clientFactory = clientFactory;
    private readonly IIncomingAccessTokenAccessor? _accessor = accessor;

    /// <summary>Creates the client that fits the token of the current call.</summary>
    /// <returns>The user client for a request with a user token, else the service client.</returns>
    public TelemetryDefinitions.TelemetryDefinitionsClient Create() =>
        _clientFactory.CreateClient<TelemetryDefinitions.TelemetryDefinitionsClient>(
            NameFor(_accessor?.TryGetUserAccessToken() is not null));

    /// <summary>Names the client for a call.</summary>
    /// <param name="hasUserToken">Tells whether the current request has the token of a user.</param>
    /// <returns>The name of the user client for a user token, else the name of the service client.</returns>
    public static string NameFor(bool hasUserToken) =>
        hasUserToken ? TelemetryClientNames.User.Definitions : TelemetryClientNames.Definitions;
}
