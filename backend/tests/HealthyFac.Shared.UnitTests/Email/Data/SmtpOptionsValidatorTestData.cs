using System.Net;
using HealthyFac.Shared.Infrastructure.Email;

namespace HealthyFac.Shared.UnitTests.Email.Data;

public static class SmtpOptionsValidatorTestData
{
    private const string Host = "smtp.example.test";
    private const string FromAddress = "no-reply@example.test";
    private const string FromName = "HealthyFac";
    private const string UserName = "mailer";
    private const string Password = "secret";

    public static TheoryData<SmtpOptions> ValidOptions => new()
    {
        CreateOptions(),
        CreateOptions(options => options with { UserName = UserName, Password = Password }),
        CreateOptions(options => options with { Port = IPEndPoint.MinPort }),
        CreateOptions(options => options with { Port = IPEndPoint.MaxPort, Security = SmtpSecurity.SslOnConnect }),
        CreateOptions(options => options with { Security = SmtpSecurity.None }),
        CreateOptions(options => options with { TimeoutSeconds = EmailConstants.Timeout.MinSeconds }),
        CreateOptions(options => options with { TimeoutSeconds = EmailConstants.Timeout.MaxSeconds }),
    };

    public static TheoryData<SmtpOptions, string> InvalidOptionsWithExpectedError => new()
    {
        { CreateOptions(options => options with { Host = string.Empty }), EmailValidationMessages.HostRequired },
        { CreateOptions(options => options with { Host = "   " }), EmailValidationMessages.HostRequired },
        {
            CreateOptions(options => options with { Port = IPEndPoint.MinPort - 1 }),
            EmailValidationMessages.PortOutOfRange
        },
        {
            CreateOptions(options => options with { Port = IPEndPoint.MaxPort + 1 }),
            EmailValidationMessages.PortOutOfRange
        },
        {
            CreateOptions(options => options with { TimeoutSeconds = EmailConstants.Timeout.MinSeconds - 1 }),
            EmailValidationMessages.TimeoutOutOfRange
        },
        {
            CreateOptions(options => options with { TimeoutSeconds = EmailConstants.Timeout.MaxSeconds + 1 }),
            EmailValidationMessages.TimeoutOutOfRange
        },
        {
            CreateOptions(options => options with { FromAddress = "not-an-address" }),
            EmailValidationMessages.FromAddressInvalid
        },
        {
            CreateOptions(options => options with { FromAddress = string.Empty }),
            EmailValidationMessages.FromAddressInvalid
        },
        {
            CreateOptions(options => options with { UserName = UserName }),
            EmailValidationMessages.CredentialsIncomplete
        },
        {
            CreateOptions(options => options with { Password = Password }),
            EmailValidationMessages.CredentialsIncomplete
        },
    };

    private static SmtpOptions CreateOptions(Func<SmtpOptions, SmtpOptions>? change = null)
    {
        var options = new SmtpOptions { Host = Host, FromAddress = FromAddress, FromName = FromName };

        return change is null ? options : change(options);
    }
}
