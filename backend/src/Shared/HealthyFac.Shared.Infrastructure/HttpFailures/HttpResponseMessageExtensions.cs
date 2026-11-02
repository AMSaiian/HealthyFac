using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.HttpFailures;

public static class HttpResponseMessageExtensions
{
    extension(HttpResponseMessage response)
    {
        /// <summary>
        /// Throws when the response has a failure status. Call it after the handling of the status codes that the
        /// caller expects, and before the caller reads the content.
        /// </summary>
        /// <remarks>
        /// A success status returns at once and reads no body. A failure status reads at most
        /// <see cref="HttpFailureConstants.MaxBodyBytes"/> of the body. The reason comes from
        /// <see cref="HttpErrorReasonReaders.Default"/> and is cut to
        /// <see cref="HttpFailureConstants.MaxReasonLength"/> characters. A longer body gives the reason when its
        /// field is complete in the first <see cref="HttpFailureConstants.MaxBodyBytes"/>. A body that is not JSON or
        /// has no known field gives no reason, and so does a body that is cut inside the field or before it. The raw
        /// body never enters the message.
        /// </remarks>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
        /// requests.</param>
        /// <returns>A task that completes when the response has a success status.</returns>
        /// <exception cref="HttpRequestException">Thrown when the response has a failure status. It holds the
        /// status code.</exception>
        public async Task EnsureSuccessAsync(CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var reason = await ReadReasonAsync(response, cancellationToken);

            throw HttpFailureErrors.RequestFailed(
                response.RequestMessage?.Method,
                response.RequestMessage?.RequestUri,
                response.StatusCode,
                reason);
        }
    }

    private static async Task<string?> ReadReasonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[HttpFailureConstants.MaxBodyBytes + 1];
        var length = await stream.ReadAtLeastAsync(
            buffer,
            buffer.Length,
            false,
            cancellationToken);

        try
        {
            var fields = JsonTopLevelFields.Read(
                buffer.AsSpan(0, Math.Min(length, HttpFailureConstants.MaxBodyBytes)),
                length <= HttpFailureConstants.MaxBodyBytes);
            var reason = HttpErrorReasonReaders.Default.ReadReason(fields);

            return reason is { Length: > HttpFailureConstants.MaxReasonLength }
                ? reason[..HttpFailureConstants.MaxReasonLength]
                : reason;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
