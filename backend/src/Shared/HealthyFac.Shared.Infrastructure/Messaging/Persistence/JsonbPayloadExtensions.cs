using System.Text;
using System.Text.Json;

namespace HealthyFac.Shared.Infrastructure.Messaging.Persistence;

/// <summary>
/// States what the <see cref="MessagingPersistenceConstants.PayloadColumnType"/> column accepts.
/// </summary>
internal static class JsonbPayloadExtensions
{
    extension(string payload)
    {
        /// <summary>
        /// Checks that PostgreSQL can store the payload in a <c>jsonb</c> column. <c>jsonb</c> has two limits that
        /// the insert enforces. It accepts JSON text only, and it rejects the escape <c>\u0000</c> inside a string
        /// or a property name. System.Text.Json accepts that escape, so a grammar check alone leaves the case on
        /// the slow path: the insert fails, and Rebus repeats the delivery until the attempt limit. No retry can
        /// fix such a payload, so the transport rejects it at receive. A lone surrogate escape fails the same way:
        /// <c>jsonb</c> rejects it, and the string read throws <see cref="InvalidOperationException"/>. The method
        /// reads every token, so trailing text after the document also fails. It never puts the payload into an
        /// exception.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the payload is one JSON document without a null character escape;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool IsStorableAsJsonb()
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(payload));

            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName
                        && reader.ValueIsEscaped
                        && reader.GetString()!.Contains('\0'))
                    {
                        return false;
                    }
                }
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            {
                return false;
            }

            return true;
        }
    }
}
