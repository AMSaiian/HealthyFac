using Testcontainers.PostgreSql;

namespace HealthyFac.Testing.Containers;

/// <summary>
/// Runs a PostgreSQL container for integration tests. A fixture holds one instance and adds its own services and
/// schema, so every test project starts PostgreSQL the same way, with the image from <see cref="TestImages"/>.
/// </summary>
public sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(TestImages.Postgres).Build();

    /// <summary>The connection string of the running container. Read it after <see cref="StartAsync"/>.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync() => _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
