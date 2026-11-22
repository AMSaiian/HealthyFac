using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// The host environment of a service host. It is always Development, so the services apply their migrations.
/// </summary>
internal sealed class SeederHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = SeederAssembly.Instance.GetName().Name!;

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
