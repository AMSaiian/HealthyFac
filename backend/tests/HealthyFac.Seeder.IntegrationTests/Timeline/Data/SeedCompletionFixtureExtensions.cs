using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using HealthyFac.Seeder.IntegrationTests.FullSeed.Data;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Ingest.Data;
using HealthyFac.Testing.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthyFac.Seeder.IntegrationTests.Timeline.Data;

public static class SeedCompletionFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<JobResult> RunJobAsync(
            string[] args,
            CancellationToken cancellationToken,
            string? baseDirectory = null,
            TextWriter? output = null,
            DateTimeOffset? anchor = null
        )
        {
            var log = output ?? new StringWriter();
            fixture.EnterNextMoment(item: "job");
            fixture.TrackOrganization(SeedCompletionTestData.CompanyId);

            var exitCode = await SeedProgram.RunAsync(
                args,
                log,
                baseDirectory ?? AppContext.BaseDirectory,
                SeederFixture.CreateConfiguration(fixture.CreateSettings()),
                anchor ?? FullSeedTestData.SaturdayAnchor,
                cancellationToken);
            await fixture.CreateResettersAsync();

            return new JobResult { ExitCode = exitCode, Output = log.ToString()! };
        }

        public async Task AddReceivedMessageAsync(Guid id)
        {
            var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionStringOf(service: "telemetry"))
            {
                Pooling = false,
            }.ConnectionString;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(SeedCompletionTestData.InsertReceivedMessageSql, connection);
            command.Parameters.AddWithValue(parameterName: "id", id);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<DataSnapshot> TakeSnapshotAsync()
        {
            var tables = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var service in SeederTestData.ServiceNames)
            {
                var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionStringOf(service))
                {
                    Pooling = false,
                }.ConnectionString;
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                foreach (var table in await ListTablesAsync(connection))
                {
                    tables[$"{service}.{table}"] = await DescribeTableAsync(connection, table);
                }
            }

            var blobCount = await fixture.CountBlobsAsync();
            var userCount = await fixture.CountKeycloakUsersAsync();
            var mailCount = (await fixture.Mailpit.GetMessagesAsync()).Count;

            return new DataSnapshot
            {
                Tables = tables,
                Blobs = blobCount,
                KeycloakUsers = userCount,
                Mails = mailCount,
            };
        }

        public async Task<int> CountBlobsAsync()
        {
            using var scope = fixture.Hosts.Document.CreateScope(SeederTestData.InitializationActor);
            var container = scope.ServiceProvider.GetRequiredService<BlobServiceClient>()
                .GetBlobContainerClient(IngestTestData.BlobContainer);
            var count = 0;
            await foreach (var unused in container.GetBlobsAsync())
            {
                count++;
            }

            return count;
        }

        public async Task<int> CountKeycloakUsersAsync()
        {
            var token = await fixture.Keycloak.GetAdminTokenAsync();
            using var client = new HttpClient { BaseAddress = new Uri(fixture.Keycloak.BaseAddress) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", token);
            var path = string.Format(SeedCompletionTestData.UserCountPath, SeederTestData.KeycloakRealm);
            var count = await client.GetTextAsync(path, TestContext.Current.CancellationToken);

            return int.Parse(count);
        }
    }

    private static async Task<List<string>> ListTablesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(SeedCompletionTestData.ListTablesSql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(ordinal: 0));
        }

        return tables;
    }

    private static async Task<string> DescribeTableAsync(NpgsqlConnection connection, string table)
    {
        await using var hasColumn = new NpgsqlCommand(SeedCompletionTestData.HasUpdatedAtSql, connection);
        hasColumn.Parameters.AddWithValue(parameterName: "table", table);
        var isTimed = await hasColumn.ExecuteScalarAsync() is not null;
        var sql = isTimed
            ? $"""select count(*)::text || '|' || coalesce(max(updated_at)::text, '') from "{table}" """
            : $"""select count(*)::text from "{table}" """;
        await using var command = new NpgsqlCommand(sql, connection);

        return (string)(await command.ExecuteScalarAsync())!;
    }
}
