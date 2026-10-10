using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

/// <summary>
/// Reports that a request failed stateless input validation. It keeps the FluentValidation base type, so
/// the exception message and the web error payload come from FluentValidation, and it implements
/// <see cref="IRejection"/>, so tracing and logging treat the failure as an expected rejection.
/// </summary>
public class AppValidationException : ValidationException, IRejection
{
    /// <summary>
    /// Initializes a new instance with the failures that the validators reported.
    /// </summary>
    /// <param name="failures">The validation failures of the rejected request.</param>
    public AppValidationException(IEnumerable<ValidationFailure> failures)
        : base(failures)
    {
    }

    /// <summary>Null by default, because the validation decorator already logs the failures.</summary>
    public LogLevel? Level { get; init; }
}
