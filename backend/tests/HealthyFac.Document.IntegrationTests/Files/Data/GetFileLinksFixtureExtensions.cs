using System.Collections.Specialized;
using System.Web;
using HealthyFac.Document.Application.Files.Links;
using HealthyFac.Document.Infrastructure.Files.Links;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.IdEncryption;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class GetFileLinksFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public string Encrypt(Guid id) => fixture.GetSingleton<IIdEncryptor>().Encrypt(id);

        public async Task<GetFileLinksResponse> GetFileLinksAsync(
            Guid companyId,
            IEnumerable<string> ids
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetFileLinksRequest, GetFileLinksResponse>>();

            return await handler.HandleAsync(
                new GetFileLinksRequest { CompanyId = companyId, Ids = [.. ids] });
        }

        /// <summary>
        /// Builds an issuer for a test: a fixed clock and the environment of the test. Development allows the HTTP
        /// that Azurite uses.
        /// </summary>
        public SasFileLinkIssuer CreateIssuer(DateTimeOffset now, string environmentName)
        {
            var options = fixture.GetSingleton<IOptions<FileStorageOptions>>();
            var time = new FakeTimeProvider(now);
            var environment = new TestHostEnvironment { EnvironmentName = environmentName };

            return new SasFileLinkIssuer(options, time, environment, fixture.BlobService);
        }

        public Uri RebaseOnStorage(FileLink link)
        {
            var query = new Uri(link.Url).Query;
            var path =
                $"{fixture.BlobService.Uri.AbsoluteUri.TrimEnd(trimChar: '/')}/"
                + $"{DatabaseTestData.ContainerName}/{link.Id}";

            return new Uri(path + query);
        }
    }

    extension(FileLink link)
    {
        public NameValueCollection Query => HttpUtility.ParseQueryString(new Uri(link.Url).Query);
    }
}
