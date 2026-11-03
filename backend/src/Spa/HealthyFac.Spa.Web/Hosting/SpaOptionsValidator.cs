using System.Text.RegularExpressions;
using FluentValidation;
using HealthyFac.Shared.Infrastructure.Configuration;

namespace HealthyFac.Spa.Web.Hosting;

public sealed class SpaOptionsValidator : AbstractValidator<SpaOptions>
{
    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "[::1]"];

    public SpaOptionsValidator()
    {
        RuleFor(options => options.ContentRoot)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.ContentRootRequired);

        RuleFor(options => options.ApiBaseUrl)
            .Cascade(CascadeMode.Stop)
            .Must(IsAbsoluteHttpsUri)
            .WithMessage(SpaValidationMessages.ApiBaseUrlMustBeHttps)
            .Must(value => !LoopbackHosts.Contains(new Uri(value).Host, StringComparer.OrdinalIgnoreCase))
            .WithMessage(SpaValidationMessages.ApiBaseUrlMustNotBeLoopback)
            .Must(value => !value.EndsWith('/'))
            .WithMessage(SpaValidationMessages.ApiBaseUrlMustNotEndWithSlash);

        RuleFor(options => options.HashedFileNamePattern)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.HashedFileNamePatternRequired)
            .Must(IsValidRegex)
            .WithMessage(SpaValidationMessages.HashedFileNamePatternInvalid);

        RuleFor(options => options.ImmutableCacheSeconds)
            .InclusiveBetween(SpaConstants.HashedFiles.MinCacheSeconds, SpaConstants.HashedFiles.MaxCacheSeconds)
            .WithMessage(SpaValidationMessages.ImmutableCacheSecondsOutOfRange);

        RuleFor(options => options.PermissionsPolicy)
            .NotEmpty()
            .WithMessage(SpaValidationMessages.PermissionsPolicyRequired);
    }

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
