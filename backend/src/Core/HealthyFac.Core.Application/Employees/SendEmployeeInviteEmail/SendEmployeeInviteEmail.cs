using HealthyFac.Core.Application.Employees.InviteLinks;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

public sealed class SendEmployeeInviteEmailPreHandlerCheck : PassThroughPreHandlerCheck<EmployeeInvited>;

/// <summary>
/// Emails the invited person a link with a fresh token. The handler builds the token from the current salt of the
/// invite, so a resend before the first email leaves no dead link. The token never enters the database, the outbox,
/// a log message, or an exception message.
/// </summary>
/// <remarks>
/// Sending the email is the only durable step, and it is the last step. A stop or a crash after the send and
/// before the inbox commit sends the email again on the next attempt. Both links work, because each one is built
/// from the same current salt. The handler skips an invite that is missing, accepted, or expired and completes, so
/// the message does not retry forever. A sender failure propagates, so the inbox schedules a retry.
/// </remarks>
/// <param name="dbContext">Reads the invite.</param>
/// <param name="tokenService">Creates the link token.</param>
/// <param name="emailSender">Sends the email.</param>
/// <param name="emailBuilder">Builds the email from the link and the expiry time.</param>
/// <param name="linkOptions">Holds the base URL of the web app.</param>
/// <param name="timeProvider">Provides the current time.</param>
/// <param name="logger">Logs skipped invites.</param>
public sealed class SendEmployeeInviteEmailRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IEmailSender emailSender,
    IEmployeeInviteEmailBuilder emailBuilder,
    IOptions<InviteLinkOptions> linkOptions,
    TimeProvider timeProvider,
    ILogger<SendEmployeeInviteEmailRequestHandler> logger
) : RequestHandler<EmployeeInvited>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IInviteTokenService _tokenService = tokenService;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IEmployeeInviteEmailBuilder _emailBuilder = emailBuilder;
    private readonly IOptions<InviteLinkOptions> _linkOptions = linkOptions;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<SendEmployeeInviteEmailRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(EmployeeInvited request, CancellationToken cancellationToken)
    {
        var invite = await _dbContext.EmployeeInvites
            .AsNoTracking()
            .SingleOrDefaultAsync(invite => invite.Id == request.InviteId, cancellationToken);

        if (invite is null)
        {
            _logger.LogInformation(
                message: "Invite email skipped: invite {InviteId} does not exist.", request.InviteId);

            return;
        }

        if (!invite.CanAccept(_timeProvider.GetUtcNow().UtcDateTime))
        {
            _logger.LogInformation(
                message: "Invite email skipped: invite {InviteId} is accepted or expired. A supervisor must resend it.",
                invite.Id);

            return;
        }

        var model = new EmployeeInviteEmailModel
        {
            To = invite.Email,
            Link = BuildLink(_tokenService.CreateToken(invite.Id, invite.TokenSalt)),
            ExpiresAt = invite.ExpiresAt,
        };
        var email = await _emailBuilder.BuildAsync(model, cancellationToken);

        await _emailSender.SendAsync(email, cancellationToken);
    }

    private string BuildLink(string token) =>
        $"{_linkOptions.Value.SpaBaseUrl.TrimEnd(LinkConstants.PathSeparator)}"
        + $"{InviteLinkConstants.InvitePath}#{InviteLinkConstants.TokenFragmentKey}={token}";
}
