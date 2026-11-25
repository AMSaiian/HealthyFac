using System.Globalization;
using FluentValidation.Results;
using Google.Protobuf.WellKnownTypes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Reads the text fields of a gRPC request. The caller sends plain GUIDs, dates, and enumeration values as text.
/// </summary>
public static class RpcInput
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Reads an ID. A text that is not a GUID names no entity, so it gets the answer of an unknown entity.
    /// </summary>
    /// <param name="id">The ID as text.</param>
    /// <returns>The ID.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="id"/> is not a GUID.</exception>
    public static Guid ParseId(string id) =>
        Guid.TryParse(id, out var parsed) ? parsed : throw new NotFoundException(RpcErrors.IdInvalid);

    /// <summary>
    /// Reads an optional ID: an empty text means "no value".
    /// </summary>
    /// <param name="id">The ID as text, or an empty text.</param>
    /// <returns>The ID, or <see langword="null"/> for an empty text.</returns>
    /// <exception cref="NotFoundException">Thrown when <paramref name="id"/> is set and is not a GUID.</exception>
    public static Guid? ParseOptionalId(string id) => id.Length == 0 ? null : ParseId(id);

    /// <summary>
    /// Reads an optional text field like the shared JSON options do over HTTP: it trims and normalizes the text. A text
    /// that is empty after that means "no value".
    /// </summary>
    /// <param name="text">The text of the field.</param>
    /// <returns>The normalized text, or <see langword="null"/> when nothing remains.</returns>
    public static string? ReadText(string text)
    {
        var normalized = TextInputPolicy.Normalize(text);

        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Reads a date in the form <c>yyyy-MM-dd</c>.
    /// </summary>
    /// <param name="date">The date as text.</param>
    /// <returns>The date.</returns>
    /// <exception cref="AppValidationException">Thrown when <paramref name="date"/> has another form.</exception>
    public static DateOnly ParseDate(string date) =>
        DateOnly.TryParseExact(date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw Invalid(nameof(date), RpcErrors.DateInvalid);

    /// <summary>
    /// Reads the values of an enumeration.
    /// </summary>
    /// <param name="values">The values as text.</param>
    /// <param name="property">The name of the field, for the failure.</param>
    /// <typeparam name="TEnum">The enumeration.</typeparam>
    /// <returns>The members.</returns>
    /// <exception cref="AppValidationException">Thrown when a value is not a member.</exception>
    public static List<TEnum> ParseEnumeration<TEnum>(IEnumerable<string> values, string property)
        where TEnum : Enumeration<TEnum> =>
    [
        .. values.Select(value => Enumeration<TEnum>.TryFromValue(value, out var member)
            ? member
            : throw Invalid(property, RpcErrors.EnumerationInvalid)),
    ];

    /// <summary>
    /// Reads a required timestamp. A message field that the caller left out arrives as <see langword="null"/>.
    /// </summary>
    /// <param name="timestamp">The timestamp of the field.</param>
    /// <param name="property">The name of the field, for the failure.</param>
    /// <returns>The time in UTC.</returns>
    /// <exception cref="AppValidationException">Thrown when <paramref name="timestamp"/> is missing.</exception>
    public static DateTime ParseRequiredTimestamp(Timestamp? timestamp, string property) =>
        timestamp?.ToDateTime() ?? throw Invalid(property, RpcErrors.TimestampRequired);

    public static string ToDateText(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string ToLocalTimeText(TimeOnly time) => time.ToString(format: "HH:mm", CultureInfo.InvariantCulture);

    public static string ToLocalDateTimeText(DateTime time) =>
        time.ToString(format: "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Builds the failure for a field that holds a value the request cannot use.
    /// </summary>
    /// <param name="property">The name of the field.</param>
    /// <param name="message">The reason.</param>
    /// <returns>The exception that the interceptor maps to <c>InvalidArgument</c>.</returns>
    public static AppValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
