using System.Collections.Frozen;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HealthyFac.Shared.Application.ErrorHandling;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Answers the application exceptions that carry a known status. It declines anything else, so the
/// generic handler can answer it.
/// </summary>
public class ApplicationExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Registers one builder per application exception type. Adding an exception means adding one entry
    /// and one method.
    /// </summary>
    private static readonly FrozenDictionary<Type, Func<Exception, ProblemDetails>> Builders =
        new Dictionary<Type, Func<Exception, ProblemDetails>>
        {
            [typeof(ValidationException)] = CreateValidation,
            [typeof(NotFoundException)] = CreateNotFound,
            [typeof(UnauthorizedAccessException)] = CreateUnauthorized,
            [typeof(ForbiddenAccessException)] = CreateForbidden,
            [typeof(ConflictException)] = CreateConflict,
            [typeof(UnprocessableException)] = CreateUnprocessable,
        }.ToFrozenDictionary();

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var builder = ResolveBuilder(exception.GetType());
        if (builder is null)
        {
            return false;
        }

        await httpContext.Response.WriteProblemDetailsAsync(builder(exception), cancellationToken);

        return true;
    }

    /// <summary>
    /// Builds the response for a validation failure, adding the per-property failures.
    /// </summary>
    /// <param name="exception">The validation exception carrying the failures.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not a <see cref="ValidationException"/>.
    /// </exception>
    private static ProblemDetails CreateValidation(Exception exception)
    {
        if (exception is not ValidationException validationException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(ValidationException),
                exception.GetType());
        }

        return new ValidationProblemDetails(new ValidationResult(validationException.Errors).ToDictionary())
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ProblemDetailsConstants.Validation.Title,
            Type = ProblemDetailsConstants.Validation.TypeUrl,
            Detail = ValidationErrorConstants.DefaultErrorMessage,
        };
    }

    /// <summary>
    /// Builds the response for a missing resource.
    /// </summary>
    /// <param name="exception">The not-found exception to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not a <see cref="NotFoundException"/>.
    /// </exception>
    private static ProblemDetails CreateNotFound(Exception exception)
    {
        if (exception is not NotFoundException notFoundException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(NotFoundException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = ProblemDetailsConstants.NotFound.Title,
            Type = ProblemDetailsConstants.NotFound.TypeUrl,
            Detail = notFoundException.Message,
        };
    }

    /// <summary>
    /// Builds the response for a missing or invalid credential.
    /// </summary>
    /// <param name="exception">The unauthorized exception to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not an <see cref="UnauthorizedAccessException"/>.
    /// </exception>
    private static ProblemDetails CreateUnauthorized(Exception exception)
    {
        if (exception is not UnauthorizedAccessException unauthorizedException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(UnauthorizedAccessException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = ProblemDetailsConstants.Unauthorized.Title,
            Type = ProblemDetailsConstants.Unauthorized.TypeUrl,
            Detail = unauthorizedException.Message,
        };
    }

    /// <summary>
    /// Builds the response for an authenticated caller without permission.
    /// </summary>
    /// <param name="exception">The forbidden exception to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not a <see cref="ForbiddenAccessException"/>.
    /// </exception>
    private static ProblemDetails CreateForbidden(Exception exception)
    {
        if (exception is not ForbiddenAccessException forbiddenException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(ForbiddenAccessException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = ProblemDetailsConstants.Forbidden.Title,
            Type = ProblemDetailsConstants.Forbidden.TypeUrl,
            Detail = forbiddenException.Message,
        };
    }

    /// <summary>
    /// Builds the response for a request that conflicts with the current state.
    /// </summary>
    /// <param name="exception">The conflict exception to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not a <see cref="ConflictException"/>.
    /// </exception>
    private static ProblemDetails CreateConflict(Exception exception)
    {
        if (exception is not ConflictException conflictException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(ConflictException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = ProblemDetailsConstants.Conflict.Title,
            Type = ProblemDetailsConstants.Conflict.TypeUrl,
            Detail = conflictException.Message,
        };
    }

    /// <summary>
    /// Builds the response for a well-formed request that the domain cannot process.
    /// </summary>
    /// <param name="exception">The unprocessable exception to report.</param>
    /// <returns>The problem details to write.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="exception"/> is not an <see cref="UnprocessableException"/>.
    /// </exception>
    private static ProblemDetails CreateUnprocessable(Exception exception)
    {
        if (exception is not UnprocessableException unprocessableException)
        {
            throw ErrorHandlingErrors.UnsupportedExceptionType(
                typeof(UnprocessableException),
                exception.GetType());
        }

        return new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = ProblemDetailsConstants.Unprocessable.Title,
            Type = ProblemDetailsConstants.Unprocessable.TypeUrl,
            Detail = unprocessableException.Message,
        };
    }

    /// <summary>
    /// Finds the builder for an exception type, then for each base type in turn.
    /// Walking the hierarchy keeps a derived application exception mapped like its base.
    /// </summary>
    /// <param name="exceptionType">The runtime type of the thrown exception.</param>
    /// <returns>The builder, or <see langword="null"/> when no base type is registered.</returns>
    private static Func<Exception, ProblemDetails>? ResolveBuilder(Type? exceptionType)
    {
        for (var current = exceptionType; current is not null; current = current.BaseType)
        {
            if (Builders.TryGetValue(current, out var builder))
            {
                return builder;
            }
        }

        return null;
    }
}
