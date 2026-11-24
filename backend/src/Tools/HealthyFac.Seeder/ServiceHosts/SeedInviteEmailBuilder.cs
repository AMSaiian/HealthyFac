using System.Globalization;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// Builds the invite email of the seed as plain text. The Razor builder lives in the Core web project, which the
/// seed job does not reference. The email only has to reach the mail catcher with the invite link.
/// </summary>
internal sealed class SeedInviteEmailBuilder : IEmployeeInviteEmailBuilder
{
    public Task<EmailMessage> BuildAsync(EmployeeInviteEmailModel model, CancellationToken cancellationToken)
    {
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"{SeedInviteEmailText.Body} {model.Link} ({SeedInviteEmailText.ExpiryPrefix} {model.ExpiresAt:u})");

        return Task.FromResult(
            new EmailMessage
            {
                To = model.To,
                Subject = SeedInviteEmailText.Subject,
                TextBody = text,
                HtmlBody = text,
            });
    }
}
