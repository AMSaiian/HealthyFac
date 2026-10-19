using HealthyFac.Shared.Application.Time;

namespace HealthyFac.Shared.Infrastructure.Time;

public class DateProvider : IDateProvider
{
    public DateTime GetUtc() => DateTime.UtcNow;
}
