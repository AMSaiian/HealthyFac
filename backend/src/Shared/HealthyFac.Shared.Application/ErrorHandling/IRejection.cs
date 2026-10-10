using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

/// <summary>
/// Marks an exception that rejects a request because of the caller's input, identity, or the current state
/// of the data. A rejection is an expected outcome, not a system failure. Tracing leaves the span status
/// unset for it, and the web layer answers with a 4xx status. Implement it on any exception, whatever its
/// base class.
/// </summary>
public interface IRejection
{
    /// <summary>
    /// The level at which the use case logs the rejection, or null to skip the log entry. The default is
    /// <see cref="LogLevel.Information"/>. A null level fits a rejection that another component already logs.
    /// Override it on an exception type, or carry it as an instance value, when a rejection deserves
    /// attention, for example a repeated access violation.
    /// </summary>
    LogLevel? Level => LogLevel.Information;
}
