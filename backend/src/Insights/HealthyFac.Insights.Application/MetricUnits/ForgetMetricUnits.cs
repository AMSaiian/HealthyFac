using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Messaging.MetricUnits;

namespace HealthyFac.Insights.Application.MetricUnits;

/// <summary>
/// The unit catalog is global and holds no company data, so the check has no scope rule.
/// </summary>
public sealed class ForgetMetricUnitsPreHandlerCheck : PassThroughPreHandlerCheck<MetricUnitsChanged>;

/// <summary>
/// Drops the cached unit catalog when Telemetry reports a change.
/// </summary>
/// <param name="cache">Deletes the cached copy.</param>
public sealed class ForgetMetricUnitsRequestHandler(IMetricUnitCache cache) : RequestHandler<MetricUnitsChanged>
{
    private readonly IMetricUnitCache _cache = cache;

    protected override Task HandleInternalAsync(MetricUnitsChanged request, CancellationToken cancellationToken) =>
        _cache.ForgetAsync(cancellationToken);
}
