using Azure.Storage.Blobs;
using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Files.Storage;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HealthyFac.Seeder.Reset;

/// <summary>
/// Clears everything that the seed created, so the job can seed again. The order: refuse when another process uses a
/// database, delete the received message that marks the telemetry history, delete the completion marker draft in
/// Core, delete the Keycloak organization with every member, delete every blob of the Document container, and empty
/// the tables of each database with one <c>TRUNCATE</c> in one transaction.
/// </summary>
/// <remarks>
/// The reset resumes after a stop: each step is safe to repeat (a missing organization, a missing blob, and an empty
/// table are no errors), and a stop never leaves a half emptied database, because the truncate of one database is one
/// transaction that the token rolls back. The marker message goes first and the marker draft second, so a reset that
/// stops at any later step leaves a seed that reads as incomplete (<see cref="Timeline.SeedState.Incomplete"/>). The
/// next normal start then stops with the reset hint, and the next run of the reset finishes the work.
/// </remarks>
public sealed class SeedReset
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly KeycloakAccounts _accounts;
    private readonly Guid _companyId;
    private readonly Guid _markerDraftId;
    private readonly Guid _markerMessageId;
    private readonly TextWriter _output;

    public SeedReset(
        ServiceHosts.ServiceHosts hosts,
        KeycloakAccounts accounts,
        Guid companyId,
        Guid markerDraftId,
        Guid markerMessageId,
        TextWriter output
    )
    {
        _hosts = hosts;
        _accounts = accounts;
        _companyId = companyId;
        _markerDraftId = markerDraftId;
        _markerMessageId = markerMessageId;
        _output = output;
    }

    /// <summary>Runs after the marker message is deleted. A test sets it to stop the reset at that point.</summary>
    internal Func<Task>? AfterMarkerMessageDeleted { get; init; }

    /// <summary>Runs after the marker draft is deleted. A test sets it to stop the reset at that point.</summary>
    internal Func<Task>? AfterMarkerDeleted { get; init; }

    /// <summary>Runs the reset.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="false"/> when another process uses a database (the reason is in the log and nothing
    /// changed); <see langword="true"/> when the reset finished.</returns>
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var targets = CreateTargets();
        var busyDatabase = await FindBusyDatabaseAsync(targets, cancellationToken);

        if (busyDatabase is not null)
        {
            await _output.WriteLineAsync(ResetErrors.DatabaseInUse(busyDatabase));

            return false;
        }

        await DeleteMarkerMessageAsync(cancellationToken);

        if (AfterMarkerMessageDeleted is not null)
        {
            await AfterMarkerMessageDeleted();
        }

        cancellationToken.ThrowIfCancellationRequested();

        await DeleteMarkerDraftAsync(cancellationToken);

        if (AfterMarkerDeleted is not null)
        {
            await AfterMarkerDeleted();
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _accounts.DeleteOrganizationWithMembersAsync(_companyId, cancellationToken);
        await DeleteBlobsAsync(cancellationToken);

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await TruncateAsync(target, cancellationToken);
        }

        return true;
    }

    private async Task DeleteMarkerMessageAsync(CancellationToken cancellationToken)
    {
        using var scope = _hosts.Telemetry.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Telemetry));
        var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

        await dbContext.ReceivedMessages
            .Where(message => message.Id == _markerMessageId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task DeleteMarkerDraftAsync(CancellationToken cancellationToken)
    {
        using var scope = _hosts.Core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core));
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        await dbContext.MachineDrafts
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(draft => draft.Id == _markerDraftId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private IReadOnlyList<ResetTarget> CreateTargets() =>
    [
        new() { Host = _hosts.Core, GetContext = provider => provider.GetRequiredService<CoreDbContext>() },
        new() { Host = _hosts.Telemetry, GetContext = provider => provider.GetRequiredService<TelemetryDbContext>() },
        new() { Host = _hosts.Document, GetContext = provider => provider.GetRequiredService<DocumentDbContext>() },
        new() { Host = _hosts.Insights, GetContext = provider => provider.GetRequiredService<InsightsDbContext>() },
        new() { Host = _hosts.Auth, GetContext = provider => provider.GetRequiredService<AuthDbContext>() },
    ];

    private async Task<string?> FindBusyDatabaseAsync(
        IReadOnlyList<ResetTarget> targets,
        CancellationToken cancellationToken
    )
    {
        var names = targets.Select(ReadDatabaseName).ToArray();

        using var scope = targets[0]
            .Host.CreateScope(
                ServiceActor.DatabaseInitialization.For(targets[0].Host.Name));

        var dbContext = targets[0].GetContext(scope.ServiceProvider);
        await using var command = new NpgsqlCommand(ResetConstants.BusySessionSql);
        command.Parameters.AddWithValue(ResetConstants.NamesParameter, names);
        command.Parameters.AddWithValue(ResetConstants.OwnParameter, SeederProcess.ApplicationName);
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        command.Connection = connection;

        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    private static string ReadDatabaseName(ResetTarget target)
    {
        using var scope = target.Host.CreateScope(ServiceActor.DatabaseInitialization.For(target.Host.Name));

        return target.GetContext(scope.ServiceProvider).Database.GetDbConnection().Database;
    }

    private async Task DeleteBlobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _hosts.Document.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Document));

        var containerName = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>()
            .Value.ContainerName;

        var container = scope.ServiceProvider.GetRequiredService<BlobServiceClient>()
            .GetBlobContainerClient(containerName);

        var isPresent = await container.ExistsAsync(cancellationToken);

        if (!isPresent.Value)
        {
            return;
        }

        await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            await container.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: cancellationToken);
        }
    }

    private static async Task TruncateAsync(ResetTarget target, CancellationToken cancellationToken)
    {
        using var scope = target.Host.CreateScope(ServiceActor.DatabaseInitialization.For(target.Host.Name));
        var dbContext = target.GetContext(scope.ServiceProvider);
        var tables = TruncateTables.List(dbContext.Model, TruncateKeepList.For(target.Host.Name));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(TruncateTables.BuildSql(tables), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private sealed record ResetTarget
    {
        public required ServiceHost Host { get; init; }

        public required Func<IServiceProvider, DbContext> GetContext { get; init; }
    }
}
