namespace HealthyFac.Seeder.Accounts;

/// <summary>
/// The password policy of the Keycloak realm (<c>passwordPolicy</c> in <c>healthyfac-realm.json</c>). The seed check
/// applies it to every seed password, so the job never reaches Keycloak with a password that the realm rejects.
/// </summary>
public static class RealmPasswordPolicy
{
    public const int MinLength = 12;

    /// <summary>The policy text of the realm file.</summary>
    public static readonly string Text = $"length({MinLength}) and notUsername";

    /// <summary>Checks a password against the policy.</summary>
    /// <param name="username">The username of the account.</param>
    /// <param name="password">The password.</param>
    /// <returns>The text of the broken rule, or <see langword="null"/> when the password follows the policy.</returns>
    public static string? FindViolation(string username, string password)
    {
        if (password.Length < MinLength)
        {
            return AccountErrors.PasswordTooShort(MinLength);
        }

        return string.Equals(password, username, StringComparison.OrdinalIgnoreCase)
            ? AccountErrors.PasswordEqualsUsername
            : null;
    }
}
