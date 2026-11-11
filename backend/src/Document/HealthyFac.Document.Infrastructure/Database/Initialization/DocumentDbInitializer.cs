using HealthyFac.Document.Infrastructure.Database.Seeding;
using HealthyFac.Document.Infrastructure.Files.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Document.Infrastructure.Database.Initialization;

/// <summary>
/// Initializes the document database and performs data seeding operations.
/// </summary>
public class DocumentDbInitializer(
    ILogger<DocumentDbInitializer> logger,
    DocumentDbContext dbContext,
    IHostEnvironment environment,
    DocumentDevelopmentDataSeeder developmentDataSeeder,
    DemoFileSeeder demoFileSeeder,
    BlobFileStorage fileStorage
) : IDocumentDbInitializer
{
    private readonly ILogger<DocumentDbInitializer> _logger = logger;
    private readonly DocumentDbContext _dbContext = dbContext;
    private readonly IHostEnvironment _environment = environment;
    private readonly DocumentDevelopmentDataSeeder _developmentDataSeeder = developmentDataSeeder;
    private readonly DemoFileSeeder _demoFileSeeder = demoFileSeeder;
    private readonly BlobFileStorage _fileStorage = fileStorage;

    /// <summary>
    /// Applies database migrations to the document database context, only in the Development
    /// environment. Other environments apply migrations in a deployment step, so a service never changes
    /// a shared schema while it starts.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyDatabaseStructureAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation(DatabaseConstants.Messages.MigrationSkipped, _environment.EnvironmentName);

            return;
        }

        try
        {
            if (_dbContext.Database.IsRelational())
            {
                _logger.LogInformation(DatabaseConstants.Messages.MigrationApplying);
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, DatabaseConstants.Errors.Migration);
            throw;
        }
    }

    /// <summary>
    /// Creates the blob container when it is missing.
    /// </summary>
    /// <remarks>
    /// A failure logs a warning and does not stop the start, because the file storage is a <c>Degraded</c> dependency:
    /// the service still serves the file grids, and the first upload then fails with status 503.
    /// </remarks>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedRequiredDataAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            await _fileStorage.EnsureContainerAsync(cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, DatabaseConstants.Messages.ContainerNotCreated);
        }
    }

    /// <summary>
    /// Seeds the registrations of the seed machines, then the demo files, only in the Development environment.
    /// It adds nothing that exists, so a restart does not duplicate the data.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedOptionalDataAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        await _developmentDataSeeder.SeedAsync(cancellationToken);
        if (_environment.IsDevelopment())
        {
            await _demoFileSeeder.SeedAsync(cancellationToken);
        }
    }
}
