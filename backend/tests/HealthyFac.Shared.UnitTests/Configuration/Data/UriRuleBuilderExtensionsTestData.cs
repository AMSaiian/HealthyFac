using FluentValidation;
using HealthyFac.Shared.Application.Configuration;

namespace HealthyFac.Shared.UnitTests.Configuration.Data;

public static class UriRuleBuilderExtensionsTestData
{
    public static TheoryData<string> AbsoluteHttpUris => new()
    {
        "https://auth.healthyfac.test/realms/healthyfac",
        "http://keycloak:8080/realms/healthyfac",
    };

    public static TheoryData<string> OtherValues => new()
    {
        string.Empty,
        "/relative/path",
        "ftp://files.healthyfac.test/export",
    };

    public static InlineValidator<UriSetting> CreateValidator() =>
        new() { validator => validator.RuleFor(setting => setting.Value).MustBeAbsoluteHttpUri() };
}

public sealed record UriSetting
{
    public required string Value { get; init; }
}
