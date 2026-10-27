using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace HealthyFac.Gateway.Web.Proxy;

public static class GatewayProblemDetailsExtensions
{
    private const string TraceIdKey = "traceId";

    extension(HttpResponse response)
    {
        /// <summary>
        /// Writes an <c>application/problem+json</c> body, like the services do, and sets the status code.
        /// The body never carries exception details.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="title">The short, fixed title of the error.</param>
        /// <param name="typeUrl">The URL that identifies the error type.</param>
        /// <param name="detail">The fixed explanation for the client.</param>
        /// <param name="cancellationToken">The token that cancels the write.</param>
        /// <returns>A task that completes when the body is written.</returns>
        public Task WriteGatewayProblemAsync(
            int statusCode,
            string title,
            string typeUrl,
            string detail,
            CancellationToken cancellationToken
        )
        {
            response.StatusCode = statusCode;

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = typeUrl,
                Detail = detail,
                Extensions = { [TraceIdKey] = response.HttpContext.TraceIdentifier },
            };

            return response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: MediaTypeNames.Application.ProblemJson,
                cancellationToken);
        }
    }
}
