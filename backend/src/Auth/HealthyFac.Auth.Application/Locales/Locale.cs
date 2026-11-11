using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Auth.Application.Locales;

/// <summary>
/// Lists the locales that a user may choose. The web app and the login theme offer the same languages. The value is
/// the code that Keycloak stores in the <c>locale</c> user attribute.
/// </summary>
public sealed class Locale : Enumeration<Locale>
{
    public static readonly Locale English = new(value: "en");
    public static readonly Locale Ukrainian = new(value: "uk");

    private Locale(string value)
        : base(value)
    {
    }
}
