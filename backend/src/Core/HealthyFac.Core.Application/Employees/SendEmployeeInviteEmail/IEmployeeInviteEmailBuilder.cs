using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

/// <summary>
/// Builds the invite email from a link and an expiry time. The Application layer does not render markup, so the
/// Web layer provides the implementation.
/// </summary>
public interface IEmployeeInviteEmailBuilder
{
    /// <summary>
    /// Builds the email with a text body and an HTML body.
    /// </summary>
    /// <param name="model">The address of the invited person, the link, and the expiry time to show.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>The email, ready to send.</returns>
    Task<EmailMessage> BuildAsync(EmployeeInviteEmailModel model, CancellationToken cancellationToken);
}
