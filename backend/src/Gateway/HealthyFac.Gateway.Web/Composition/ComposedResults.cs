using System.Net.Mime;
using System.Text.Json.Nodes;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Builds the HTTP results of the composed endpoints.
/// </summary>
public static class ComposedResults
{
    /// <summary>
    /// Builds a problem details result with the status and the body of a service, or of the Gateway.
    /// </summary>
    /// <param name="status">The HTTP status code.</param>
    /// <param name="problem">The problem details body.</param>
    /// <returns>The result with the <c>application/problem+json</c> content type.</returns>
    public static IResult Problem(int status, JsonNode problem) =>
        Results.Json(problem, options: null, contentType: MediaTypeNames.Application.ProblemJson, statusCode: status);

    /// <summary>
    /// Builds the <c>400</c> result for a request body that the Gateway rejects itself.
    /// </summary>
    /// <param name="errors">The messages of each invalid field.</param>
    /// <returns>The validation problem result.</returns>
    public static IResult Validation(IReadOnlyDictionary<string, string[]> errors) =>
        Problem(StatusCodes.Status400BadRequest, ComposedErrors.CreateValidationProblem(errors));
}
