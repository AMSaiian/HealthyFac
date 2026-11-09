using HealthyFac.Document.Domain.MachineRegistrations;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Document.Infrastructure.Database.Seeding;

/// <summary>
/// Creates the registrations of the seed machines. Core publishes the machines of the same seed data file only after
/// it starts, so the seeder creates the registrations itself. The seeder skips a machine whose registration exists, so
/// a second start adds nothing. The registration is the only trigger, so the later <c>MachineCreated</c> message
/// finds the row and changes nothing. The seeder creates no files.
/// </summary>
/// <param name="dbContext">The document database context.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
public sealed class DocumentDevelopmentDataSeeder(DocumentDbContext dbContext, IOptions<SeedingOptions> seedingOptions)
{
    private readonly DocumentDbContext _dbContext = dbContext;
    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;

    /// <summary>
    /// Adds the missing registrations of the seed machines in one save.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.TestTenant is not { } tenant)
        {
            return;
        }

        var seeds = _options.GetSeedMachines();
        var seedIds = seeds.Select(seed => seed.MachineId).ToList();
        var registeredIds = await _dbContext.MachineRegistrations
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .Where(registration => seedIds.Contains(registration.Id))
            .Select(registration => registration.Id)
            .ToListAsync(cancellationToken);

        _dbContext.MachineRegistrations.AddRange(
            seeds.Where(seed => !registeredIds.Contains(seed.MachineId))
                .Select(seed => new MachineRegistration(seed.MachineId, tenant.CompanyId)));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
