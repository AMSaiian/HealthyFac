using FluentValidation.Results;
using HealthyFac.Document.Application.Files;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Normalizes the text fields of a multipart form. The shared JSON options normalize the strings of a JSON body; a form
/// string takes another path, so this class applies the same policy (<see cref="TextInputPolicy.Normalize"/>). A
/// browser sends each line break of a form value as CRLF, so without it a description of two lines would fail the text
/// rule.
/// </summary>
public static class MultipartText
{
    /// <summary>
    /// Normalizes a form string.
    /// </summary>
    /// <param name="value">The text as received, or <see langword="null"/>.</param>
    /// <param name="key">The name of the field in the error.</param>
    /// <returns>The normalized text, or <see langword="null"/> when <paramref name="value"/> is
    /// <see langword="null"/>.</returns>
    /// <exception cref="AppValidationException">Thrown when the text is not valid Unicode (e.g., a lone
    /// surrogate).</exception>
    public static string? Normalize(string? value, string key)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return TextInputPolicy.Normalize(value);
        }
        catch (ArgumentException)
        {
            throw new AppValidationException([new ValidationFailure(key, FileErrors.InvalidText)]);
        }
    }
}
