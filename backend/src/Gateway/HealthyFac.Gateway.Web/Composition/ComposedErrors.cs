using System.Text.Json.Nodes;
using HealthyFac.Gateway.Web.Proxy;

namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Combines the results of the downstream calls of one composed endpoint into one outcome, so the person sees one
/// error, as if one service had answered.
/// </summary>
public static class ComposedErrors
{
    private const string ErrorsProperty = "errors";
    private const string TypeProperty = "type";
    private const string TitleProperty = "title";
    private const string StatusProperty = "status";
    private const string DetailProperty = "detail";

    private static readonly int[] StatusPriority =
    [
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status400BadRequest,
    ];

    /// <summary>
    /// Combines the results. The order of <paramref name="results"/> is the order of the services (Core first), and the
    /// order of the bodies of a success.
    /// <list type="bullet">
    /// <item>Any <see cref="DownstreamResult.Unavailable"/> gives <c>503</c>, because the composed answer would be
    /// incomplete.</item>
    /// <item>Two <c>400</c> answers give one validation problem with the union of their <c>errors</c>.</item>
    /// <item>Different <c>4xx</c> answers give the first of 401, 403, 404, 409, 400, then any other, with its
    /// body.</item>
    /// <item>The same <c>4xx</c> other than <c>400</c> gives the first body.</item>
    /// </list>
    /// </summary>
    /// <param name="results">The results of the calls, in the order of the services.</param>
    /// <returns>The success with every body, or the failure that the Gateway answers.</returns>
    public static ComposedOutcome Combine(IReadOnlyList<DownstreamResult> results)
    {
        if (results.Any(result => result is DownstreamResult.Unavailable))
        {
            return new ComposedOutcome.Failure
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Problem = CreateUnavailableProblem(),
            };
        }

        var errors = results.OfType<DownstreamResult.ClientError>().ToList();

        if (errors.Count == 0)
        {
            return new ComposedOutcome.Success
            {
                Bodies = [.. results.OfType<DownstreamResult.Success>().Select(success => success.Body)],
            };
        }

        var isValidationOnly = errors.Count > 1 && errors.All(error => error.Status == StatusCodes.Status400BadRequest);

        if (isValidationOnly)
        {
            return new ComposedOutcome.Failure
            {
                Status = StatusCodes.Status400BadRequest,
                Problem = MergeValidation(errors),
            };
        }

        var chosen = errors
            .OrderBy(error => PriorityOf(error.Status))
            .First();

        return new ComposedOutcome.Failure { Status = chosen.Status, Problem = chosen.Problem };
    }

    /// <summary>
    /// Builds the validation problem for a request that the Gateway rejects itself.
    /// </summary>
    /// <param name="errors">The messages of each invalid field.</param>
    /// <returns>The problem details body with status <c>400</c>.</returns>
    public static JsonObject CreateValidationProblem(IReadOnlyDictionary<string, string[]> errors) =>
        new()
        {
            [TypeProperty] = GatewayProblemDetailsConstants.ValidationFailed.TypeUrl,
            [TitleProperty] = GatewayProblemDetailsConstants.ValidationFailed.Title,
            [StatusProperty] = StatusCodes.Status400BadRequest,
            [ErrorsProperty] = new JsonObject(
                errors.Select(error => KeyValuePair.Create(
                    error.Key,
                    (JsonNode?)new JsonArray([.. error.Value.Select(message => JsonValue.Create(message))])))),
        };

    private static int PriorityOf(int status)
    {
        var index = Array.IndexOf(StatusPriority, status);

        return index < 0 ? StatusPriority.Length : index;
    }

    private static JsonObject MergeValidation(IReadOnlyList<DownstreamResult.ClientError> errors)
    {
        var merged = errors[0].Problem is JsonObject first ? (JsonObject)first.DeepClone() : [];
        var union = new JsonObject();

        foreach (var error in errors)
        {
            if (error.Problem[ErrorsProperty] is not JsonObject fields)
            {
                continue;
            }

            foreach (var (key, messages) in fields)
            {
                var target = union[key] as JsonArray ?? [];
                union[key] = target;

                foreach (var message in messages?.AsArray() ?? [])
                {
                    var text = message!.GetValue<string>();

                    if (!target.Any(existing => existing!.GetValue<string>() == text))
                    {
                        target.Add(text);
                    }
                }
            }
        }

        merged[ErrorsProperty] = union;

        return merged;
    }

    private static JsonObject CreateUnavailableProblem() =>
        new()
        {
            [TypeProperty] = GatewayProblemDetailsConstants.ServiceUnavailable.TypeUrl,
            [TitleProperty] = GatewayProblemDetailsConstants.ServiceUnavailable.Title,
            [StatusProperty] = StatusCodes.Status503ServiceUnavailable,
            [DetailProperty] = CompositionErrors.DownstreamUnavailable,
        };
}
