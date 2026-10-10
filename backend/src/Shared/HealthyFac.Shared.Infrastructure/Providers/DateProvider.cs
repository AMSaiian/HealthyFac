using HealthyFac.Shared.Application.Interfaces;

namespace HealthyFac.Shared.Infrastructure.Providers;

public class DateProvider : IDateProvider
{
    public DateTime GetUtc() => DateTime.UtcNow;
}
