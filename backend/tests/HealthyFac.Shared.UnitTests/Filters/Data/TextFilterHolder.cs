using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Shared.UnitTests.Filters.Data;

public sealed record TextFilterHolder
{
    public TextFilter? Name { get; init; }
}
