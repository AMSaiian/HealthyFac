using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Infrastructure.Email;
using HealthyFac.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Email.Data;

public sealed class MailpitFixture : IAsyncLifetime
{
    private readonly MailpitTestServer _server = new();

    private ServiceProvider _services = null!;

    public MailpitTestServer Server => _server;

    public IEmailSender EmailSender => _services.GetRequiredService<IEmailSender>();

    public async Task InitializeAsync()
    {
        await _server.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{SmtpOptions.SectionName}:Host"] = _server.SmtpHost,
                    [$"{SmtpOptions.SectionName}:Port"] = _server.SmtpPort.ToString(),
                    [$"{SmtpOptions.SectionName}:Security"] = nameof(SmtpSecurity.None),
                    [$"{SmtpOptions.SectionName}:FromAddress"] = EmailTestData.FromAddress,
                    [$"{SmtpOptions.SectionName}:FromName"] = EmailTestData.FromName,
                })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmtpEmail(configuration);
        _services = services.BuildServiceProvider(validateScopes: true);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _server.DisposeAsync();
    }
}
