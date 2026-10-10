using System.Collections.Frozen;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Final handler in the chain. It answers every exception that no earlier handler claimed.
/// Framework transport failures resolve to their own status. Anything unrecognised falls back to 500.
/// </summary>
public class GenericExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GenericExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;
    private readonly FrozenDictionary<Type, Func<Exception, ProblemDetails>> _builders;

    /// <summary>
    /// Registers one builder per framework exception type.
    /// </summary>
    /// <param name="logger">The logger for the handled exception.</param>
    /// <param name="environment">The host environment deciding how much detail reaches the client.</param>
    public GenericExceptionHandler(
        ILogger<GenericExceptionHandler> logger,
        IHostEnvironment environment
    )
    {
        _logger = logger;
        _environment = environment;

        _builders = new Dictionary<Type, Func<Exception, ProblemDetails>>
        {
            [typeof(BadHttpRequestException)] = CreateBadRequest,
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Writes the problem details for the exception and logs the exception. The status decides the level, so a
    /// client fault never appears as a server error: status 500 and above log at error level, and lower
    /// statuses log at debug level.
    /// </summary>
    /// <param name="httpContext">The context of the failed request.</param>
    /// <param name="exception">The exception that no earlier handler claimed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
    /// <returns>Always <see langword="true"/>, because this handler answers every exception.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var problemDetails = ResolveBuilder(exception.GetType())(exception);
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "{Title}", problemDetails.Title);
        }
        else
        {
            _logger.LogDebug(exception, "{Title}", problemDetails.Title);
        }

        await httpContext.Response.WriteProblemDetailsAsync(problemDetails, cancellationToken);

        return true;
    }

    /// <summary>
    /// Builds the response for a malformed request. ASP.NET throws this for a malformed body, an
    /// oversized payload, or bad form content, and it carries the status itself, so an oversized payload
    /// still reports 413.
    /// </summary>
    /// <param name="exception">The transport failure to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not a <see cref="BadHttpRequestException"/>.
    /// </exception>
    private ProblemDetails CreateBadRequest(Exception exception)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(BadHttpRequestException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = badRequest.StatusCode,
            Title = ProblemDetailsConstants.BadRequest.Title,
            Type = ProblemDetailsConstants.BadRequest.TypeUrl,
            Detail = _environment.IsDevelopment()
                ? exception.ToString()
                : ProblemDetailsConstants.BadRequest.GenericDetail,
        };
    }

    /// <summary>
    /// Builds the response for any exception without its own builder.
    /// </summary>
    /// <param name="exception">The server fault to report.</param>
    /// <returns>The problem details to write.</returns>
    private ProblemDetails CreateInternalServerError(Exception exception) =>
        new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = ProblemDetailsConstants.InternalServerError.Title,
            Type = ProblemDetailsConstants.InternalServerError.TypeUrl,
            Detail = _environment.IsDevelopment()
                ? exception.ToString()
                : ProblemDetailsConstants.InternalServerError.GenericDetail,
        };

    private Func<Exception, ProblemDetails> ResolveBuilder(Type? exceptionType)
    {
        for (var current = exceptionType; current is not null; current = current.BaseType)
        {
            if (_builders.TryGetValue(current, out var builder))
            {
                return builder;
            }
        }

        return CreateInternalServerError;
    }
}
