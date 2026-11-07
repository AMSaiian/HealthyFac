using System.Text.RegularExpressions;
using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Spa.Web.Hosting;

/// <summary>
/// Configures the SPA host. Every value here is public in the browser or sets how the host serves files.
/// </summary>
public sealed class SpaOptions
{
    public const string SectionName = "Spa";

    /// <summary>
    /// The folder with the built app. A relative path starts at the application base directory.
    /// </summary>
    public required string ContentRoot { get; init; }

    /// <summary>
    /// The public URL of the API gateway. The host sends it to the browser in <c>/config.json</c> and allows it in
    /// the <c>connect-src</c> of the content security policy.
    /// </summary>
    public required string ApiBaseUrl { get; init; }

    /// <summary>
    /// The public URL of the identity provider (Keycloak). The host sends it to the browser in <c>/config.json</c>
    /// and allows its origin in the <c>connect-src</c> and <c>frame-src</c> of the content security policy.
    /// </summary>
    public required string AuthUrl { get; init; }

    /// <summary>
    /// The identity provider realm that the app signs in to.
    /// </summary>
    public required string AuthRealm { get; init; }

    /// <summary>
    /// The public client ID of the app in the identity provider realm.
    /// </summary>
    public required string AuthClientId { get; init; }

    /// <summary>
    /// The PrimeUI license key. PrimeNG checks it in the browser, so it is public by design. The host sends it to the
    /// browser in <c>/config.json</c>.
    /// </summary>
    public required string PrimeUiLicenseKey { get; init; }

    /// <summary>
    /// A .NET regular expression for the content hash in a file name (the Angular <c>outputHashing</c> part). A file
    /// that matches never changes, so it gets the immutable cache header.
    /// </summary>
    public required string HashedFileNamePattern { get; init; }

    /// <summary>
    /// The <c>max-age</c> of a hashed file in seconds.
    /// </summary>
    public required int ImmutableCacheSeconds { get; init; }

    /// <summary>
    /// The <c>max-age</c> of <c>/config.json</c> in seconds. A configuration change reaches open browsers within
    /// this time.
    /// </summary>
    public required int RuntimeConfigCacheSeconds { get; init; }

    /// <summary>
    /// The <c>Permissions-Policy</c> header value of the index document.
    /// </summary>
    public required string PermissionsPolicy { get; init; }
}

public sealed class SpaOptionsValidator : AbstractValidator<SpaOptions>
{
    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "[::1]"];

    public SpaOptionsValidator()
    {
        RuleFor(options => options.ContentRoot)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.ContentRootRequired);

        ApplyPublicHttpsUrlRule(
            RuleFor(options => options.ApiBaseUrl),
            SpaValidationMessages.ApiBaseUrlMustBeHttps,
            SpaValidationMessages.ApiBaseUrlMustNotBeLoopback,
            SpaValidationMessages.ApiBaseUrlMustNotEndWithSlash);

        ApplyPublicHttpsUrlRule(
            RuleFor(options => options.AuthUrl),
            SpaValidationMessages.AuthUrlMustBeHttps,
            SpaValidationMessages.AuthUrlMustNotBeLoopback,
            SpaValidationMessages.AuthUrlMustNotEndWithSlash);

        RuleFor(options => options.AuthRealm)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.AuthRealmRequired);

        RuleFor(options => options.AuthClientId)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.AuthClientIdRequired);

        RuleFor(options => options.PrimeUiLicenseKey)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.PrimeUiLicenseKeyRequired);

        RuleFor(options => options.HashedFileNamePattern)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.HashedFileNamePatternRequired)
            .Must(IsValidRegex)
            .WithMessage(SpaValidationMessages.HashedFileNamePatternInvalid);

        RuleFor(options => options.ImmutableCacheSeconds)
            .InclusiveBetween(SpaConstants.HashedFiles.MinCacheSeconds, SpaConstants.HashedFiles.MaxCacheSeconds)
            .WithMessage(SpaValidationMessages.ImmutableCacheSecondsOutOfRange);

        RuleFor(options => options.RuntimeConfigCacheSeconds)
            .InclusiveBetween(SpaConstants.RuntimeConfig.MinCacheSeconds, SpaConstants.RuntimeConfig.MaxCacheSeconds)
            .WithMessage(SpaValidationMessages.RuntimeConfigCacheSecondsOutOfRange);

        RuleFor(options => options.PermissionsPolicy)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.PermissionsPolicyRequired);
    }

    private static void ApplyPublicHttpsUrlRule(
        IRuleBuilderInitial<SpaOptions, string> ruleBuilder,
        string httpsMessage,
        string loopbackMessage,
        string trailingSlashMessage
    ) =>
        ruleBuilder
            .Cascade(CascadeMode.Stop)
            .Must(IsAbsoluteHttpsUri)
            .WithMessage(httpsMessage)
            .Must(value => !LoopbackHosts.Contains(new Uri(value).Host, StringComparer.OrdinalIgnoreCase))
            .WithMessage(loopbackMessage)
            .Must(value => !value.EndsWith(value: '/'))
            .WithMessage(trailingSlashMessage);

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, SpaConstants.HashedFiles.PatternMatchTimeout);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsAbsoluteHttpsUri(string? value) =>
        UriRuleBuilderExtensions.TryCreateAbsoluteHttpUri(value, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
