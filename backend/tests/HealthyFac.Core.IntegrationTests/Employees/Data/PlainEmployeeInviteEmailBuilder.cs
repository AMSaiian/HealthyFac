using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Builds an email that holds the link in the text body. <c>Core.IntegrationTests</c> does not reference
/// <c>Core.Web</c>, and the Razor rendering has its own tests.
/// </summary>
public sealed class PlainEmployeeInviteEmailBuilder : IEmployeeInviteEmailBuilder
{
    public Task<EmailMessage> BuildAsync(EmployeeInviteEmailModel model, CancellationToken cancellationToken)
    {
        var message = new EmailMessage
        {
            To = model.To,
            Subject = InviteEmailTestData.PlainSubject,
            TextBody = model.Link,
            HtmlBody = model.Link,
        };

        return Task.FromResult(message);
    }
}
