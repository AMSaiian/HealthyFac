using System.Net;
using System.Net.Http.Json;
using System.Web;
using Grpc.Net.ClientFactory;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

/// <summary>
/// Registers the Core and Telemetry clients of Insights with the service token and a user token in the flow, and
/// replaces only the network: the primary handler of each user client plays the service, and the one of the token
/// client plays Keycloak, which answers each token exchange with a token that holds the audience and the scope of
/// the form.
/// </summary>
public sealed class ChatClientRegistrationHarness : IDisposable
{
    public const string UserToken = "user-access-token";
    public const char FormSeparator = '|';

    private static readonly string[] UserClientNames =
    [
        CoreClientNames.User.Machines,
        CoreClientNames.User.WorkOrders,
        CoreClientNames.User.Employees,
        CoreClientNames.User.Schedules,
        TelemetryClientNames.User.Signals,
        TelemetryClientNames.User.Definitions,
    ];

    private readonly ServiceProvider _provider;

    public ChatClientRegistrationHarness()
    {
        var settings = new Dictionary<string, string?>(CoreClientRegistrationTestData.Settings)
        {
            ["TelemetryClient:GrpcAddress"] = "http://telemetry.test:8081",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSingleton<ITokenCacheStorage, InMemoryTokenStorage>();
        services.AddSingleton<IIncomingAccessTokenAccessor>(new UserTokenAccessor());
        services.AddServiceTokens(configuration);
        services.AddCoreClient(configuration);
        services.AddTelemetryClient(configuration);

        services.AddHttpClient(ServiceTokenConstants.TokenClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new ExchangeEndpointHandler());

        foreach (var name in UserClientNames)
        {
            services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => Service);
        }

        _provider = services.BuildServiceProvider();
    }

    public CapturingGrpcHandler Service { get; } = new();

    public GrpcClientFactory Factory => _provider.GetRequiredService<GrpcClientFactory>();

    public void Dispose() => _provider.Dispose();

    private sealed class UserTokenAccessor : IIncomingAccessTokenAccessor
    {
        public string? TryGetUserAccessToken() => UserToken;

        public Guid? TryGetUserId() => Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000d02");

        public string? TryGetUserName() => null;
    }

    private sealed class ExchangeEndpointHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var form = HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync(cancellationToken));
            var audience = form[ServiceTokenConstants.Form.Audience];
            var scope = form[ServiceTokenConstants.Form.Scope];

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { access_token = audience + FormSeparator + scope, expires_in = 300 }),
            };
        }
    }
}
