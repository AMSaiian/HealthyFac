using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Document.Infrastructure.Database;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Seeder.UnitTests.Reset.Data;

public static class TruncateTablesTestData
{
    private const string ConnectionString = "Host=localhost;Database=unit";

    public static readonly IReadOnlyList<string> MessageTables = ["outbox_messages", "inbox_messages"];

    /// <summary>One row for each context: the name, the service, and the model.</summary>
    public static TheoryData<string, ServiceName, IModel, string[]> Contexts => new()
    {
        { "core", ServiceName.Core, Model<CoreDbContext>(options => new CoreDbContext(options)), ["roles"] },
        {
            "telemetry",
            ServiceName.Telemetry,
            Model<TelemetryDbContext>(options => new TelemetryDbContext(options)),
            ["quantity_kinds", "measurement_units"]
        },
        { "document", ServiceName.Document, Model<DocumentDbContext>(options => new DocumentDbContext(options)), [] },
        { "insights", ServiceName.Insights, Model<InsightsDbContext>(options => new InsightsDbContext(options)), [] },
        { "auth", ServiceName.Auth, Model<AuthDbContext>(options => new AuthDbContext(options)), [] },
    };

    private static IModel Model<TContext>(Func<DbContextOptions<TContext>, TContext> create)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        builder.UseNpgsql(ConnectionString, npgsql => npgsql.UseVector());
        using var context = create(builder.Options);

        return context.GetService<IDesignTimeModel>().Model;
    }
}
