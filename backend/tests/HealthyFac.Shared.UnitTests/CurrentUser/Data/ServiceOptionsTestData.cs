using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Shared.UnitTests.CurrentUser.Data;

public static class ServiceOptionsTestData
{
    public static TheoryData<ServiceOptions> ValidOptions =>
        new()
        {
            new ServiceOptions { Name = "core" },
            new ServiceOptions { Name = "auth" },
            new ServiceOptions { Name = "gateway" },
        };

    public static TheoryData<ServiceOptions> InvalidOptions =>
        new()
        {
            new ServiceOptions { Name = "insights" },
            new ServiceOptions { Name = "Core" },
            new ServiceOptions { Name = string.Empty },
        };
}
