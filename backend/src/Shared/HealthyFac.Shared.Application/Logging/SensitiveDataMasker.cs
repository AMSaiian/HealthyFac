namespace HealthyFac.Shared.Application.Logging;

/// <summary>
/// Provides utility methods to mask sensitive values based on masking rules.
/// </summary>
public static class SensitiveDataMasker
{
    /// <summary>
    /// Masks the supplied value using the configuration defined in the specified attribute.
    /// </summary>
    /// <param name="value">The raw value to mask.</param>
    /// <param name="attribute">The sensitive data masking configuration attribute.</param>
    /// <returns>The masked string representation of the value, or <see langword="null"/> if the input is <see
    /// langword="null"/>.</returns>
    public static string? Mask(object? value, SensitiveDataAttribute attribute)
    {
        if (value is null)
        {
            return null;
        }

        var text = value as string ?? value.ToString();
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (attribute.ShowFirst <= 0 && attribute.ShowLast <= 0)
        {
            if (attribute.PreserveLength)
            {
                return new string(MaskingConstants.DefaultMaskChar, text.Length);
            }

            return attribute.Mask;
        }

        var requiredLength = Math.Max(0, attribute.ShowFirst) + Math.Max(0, attribute.ShowLast);
        if (text.Length <= requiredLength)
        {
            if (attribute.PreserveLength)
            {
                return new string(MaskingConstants.DefaultMaskChar, text.Length);
            }

            return attribute.Mask;
        }

        var prefix = attribute.ShowFirst > 0 ? text[..attribute.ShowFirst] : string.Empty;
        var suffix = attribute.ShowLast > 0 ? text[^attribute.ShowLast..] : string.Empty;

        if (attribute.PreserveLength)
        {
            return string.Concat(
                prefix,
                new string(MaskingConstants.DefaultMaskChar, text.Length - requiredLength),
                suffix
            );
        }

        return string.Concat(prefix, attribute.Mask, suffix);
    }
}
