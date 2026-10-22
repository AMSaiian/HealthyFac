using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = DatabaseTestData.EnvironmentName;
    public string ApplicationName { get; set; } = DatabaseTestData.ApplicationName;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public sealed class AnonymousCurrentUserProvider : ICurrentUserProvider
{
    public Guid? TryGetUserId() => null;

    public Guid GetUserId() => throw new InvalidOperationException();
}
