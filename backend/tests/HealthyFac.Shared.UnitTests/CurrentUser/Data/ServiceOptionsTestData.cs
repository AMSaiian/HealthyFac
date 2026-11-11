using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ServiceOptionsTestData
{
    public static TheoryData<ServiceOptions> ValidOptions =>
        new()
        {
            new ServiceOptions { Name = "core" },
        };

    public static TheoryData<ServiceOptions> InvalidOptions =>
        new()
        {
            new ServiceOptions { Name = "billing" },
            new ServiceOptions { Name = "Core" },
        };
}
