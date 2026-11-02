using System.Diagnostics.CodeAnalysis;

namespace HealthyFac.Shared.Domain.Emails;

/// <summary>
/// Defines the one form of an email address that every service stores and compares. Keycloak stores emails in lower
/// case too, so a comparison across services needs no other rule.
/// </summary>
public static class EmailAddresses
{
    /// <summary>
    /// Trims the address and lower-cases it with the invariant culture. A request accessor calls it, so it accepts
    /// <see langword="null"/> and never throws.
    /// </summary>
    /// <param name="value">The address as received, or <see langword="null"/>.</param>
    /// <returns>The normalized address, or <see langword="null"/> for <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value) => value?.Trim().ToLowerInvariant();
}
