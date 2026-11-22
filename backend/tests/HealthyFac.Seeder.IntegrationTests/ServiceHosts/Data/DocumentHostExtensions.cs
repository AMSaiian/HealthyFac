using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Seeder.ServiceHosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;

public static class DocumentHostExtensions
{
    public static T ReadOption<T>(this ServiceHost host, Func<FileStorageOptions, T> read)
    {
        using var scope = host.CreateScope(ServiceHostsTestData.Actor);

        return read(scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value);
    }
}
