using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Web.Rendering;

namespace HealthyFac.Core.Web.Employees.Emails;

/// <summary>
/// Builds the invite email from the Razor components <see cref="EmployeeInviteEmailHtml"/> and
/// <see cref="EmployeeInviteEmailText"/>.
/// </summary>
/// <param name="renderer">Renders the components to strings.</param>
public sealed class RazorEmployeeInviteEmailBuilder(RazorComponentRenderer renderer) : IEmployeeInviteEmailBuilder
{
    public const string Subject = "You are invited to HealthyFac";

    public async Task<EmailMessage> BuildAsync(EmployeeInviteEmailModel model, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?> { [nameof(EmployeeInviteEmailHtml.Model)] = model };
        var htmlBody = await renderer.RenderHtmlAsync<EmployeeInviteEmailHtml>(parameters, cancellationToken);
        var textBody = await renderer.RenderTextAsync<EmployeeInviteEmailText>(parameters, cancellationToken);

        return new EmailMessage
        {
            To = model.To,
            Subject = Subject,
            TextBody = textBody,
            HtmlBody = htmlBody,
        };
    }
}
