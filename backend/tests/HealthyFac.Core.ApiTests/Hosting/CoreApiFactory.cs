using HealthyFac.Shared.Application.Email;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Core.ApiTests.Hosting;

/// <summary>
/// Starts the Core host in memory against test containers. The bearer handler trusts the key of the
/// <see cref="TestTokenFactory"/>, and the email sender discards every message, so no test needs Keycloak or an
/// SMTP server.
/// </summary>
/// <remarks>
/// The settings enter twice: as host settings, and as the last configuration source. The last source wins over
/// environment variables that a <c>.env</c> file sets on a developer machine.
/// </remarks>
public sealed class CoreApiFactory(
    IReadOnlyDictionary<string, string?> settings,
    TestTokenFactory tokens
) : WebApplicationFactory<Program>
{
    private const string TestEnvironment = "Test";

    private readonly IReadOnlyDictionary<string, string?> _settings = settings;
    private readonly TestTokenFactory _tokens = tokens;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, _tokens.TrustTestKey);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, DiscardingEmailSender>();
        });
    }
}
