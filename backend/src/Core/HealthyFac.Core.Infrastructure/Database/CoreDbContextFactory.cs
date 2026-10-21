using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthyFac.Core.Infrastructure.Database;

/// <summary>
/// Builds a <see cref="CoreDbContext"/> for the Entity Framework Core tools.
/// The factory keeps migrations independent of the web host, so a missing option or an unreachable service
/// never blocks a schema change. Model configuration still comes from the context itself, so a generated
/// migration matches the running app.
/// </summary>
public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    /// <summary>
    /// Creates the context for design-time tooling.
    /// </summary>
    /// <param name="args">Arguments passed by the tooling. Unused.</param>
    /// <returns>A context configured against the connection string in the environment.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no connection string is configured. The factory never substitutes a placeholder, so a
    /// database command can never reach an unintended server.
    /// </exception>
    public CoreDbContext CreateDbContext(string[] args)
    {
        DotEnvLoader.Load();

        var connectionString = Environment.GetEnvironmentVariable(
            DatabaseConstants.ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(DatabaseConstants.Errors.MissingConnectionString);
        }

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CoreDbContext(options);
    }
}
