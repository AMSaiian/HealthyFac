using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Writes a problem details payload to a response.
/// </summary>
public static class ProblemDetailsResponseExtensions
{
    extension(HttpResponse response)
    {
        /// <summary>
        /// Writes the problem details to the response and sets the status from the payload.
        /// </summary>
        /// <remarks>
        /// The method serializes the payload as <see cref="object"/>, so the runtime type decides the shape. A
        /// generic call on the static <see cref="ProblemDetails"/> type silently drops the errors dictionary of
        /// <see cref="ValidationProblemDetails"/>.
        /// </remarks>
        /// <param name="problemDetails">The problem details to write.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous write.</returns>
        public async Task WriteProblemDetailsAsync(
            ProblemDetails problemDetails,
            CancellationToken cancellationToken
        )
        {
            response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            await response.WriteAsJsonAsync<object>(problemDetails, cancellationToken);
        }
    }
}
