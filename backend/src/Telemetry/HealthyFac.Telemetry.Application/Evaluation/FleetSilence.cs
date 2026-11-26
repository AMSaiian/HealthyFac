using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// Tells whether the whole fleet is silent. When no machine sent a message within the silence limit, the cause is
/// most likely the intake or the broker, not the machines, so the evaluation opens no silent machine episode. The
/// worker checks the fleet once before the machine runs of a round.
/// </summary>
/// <param name="options">The detection values.</param>
/// <param name="metrics">Counts the changes to silent.</param>
/// <param name="logger">Logs the change to silent.</param>
public sealed class FleetSilence(
    IOptions<DetectionOptions> options,
    DetectionMetrics metrics,
    ILogger<FleetSilence> logger
)
{
    private readonly DetectionOptions _options = options.Value;
    private readonly DetectionMetrics _metrics = metrics;
    private readonly ILogger<FleetSilence> _logger = logger;

    private volatile bool _isSilent;

    /// <summary>The result of the last check. A fleet that was never checked is not silent.</summary>
    public bool IsSilent => _isSilent;

    /// <summary>
    /// Checks whether any received message of any machine is not older than the silence limit, and stores the result.
    /// At the change from not silent to silent it logs one warning and counts one fleet silence.
    /// </summary>
    /// <param name="dbContext">Reads the received messages.</param>
    /// <param name="now">The time of the check.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns><see langword="true"/> when no machine sent a message within the silence limit.</returns>
    public async Task<bool> CheckAsync(ITelemetryDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var since = now.AddSeconds(-_options.SilenceLimitSeconds);

        var isAlive = await dbContext.ReceivedMessages
            .AsNoTracking()
            .AnyAsync(message => message.ReceivedAt >= since, cancellationToken);

        var isSilent = !isAlive;

        if (isSilent && !_isSilent)
        {
            _logger.LogWarning(
                message: "No machine sent a message within {SilenceLimitSeconds} seconds.",
                _options.SilenceLimitSeconds);

            _metrics.AddFleetSilence();
        }

        _isSilent = isSilent;

        return isSilent;
    }

    /// <summary>Puts the state back to never checked, not silent. Only the test fixtures call it.</summary>
    internal void Reset() => _isSilent = false;
}
