using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.InviteLinks;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Arranges the invite email handler and reads what it sent. A test builds the handler with its own sender, base
/// URL, or logger to reach a case that the registered handler cannot reach.
/// </summary>
public static class InviteEmailFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public SendEmployeeInviteEmailRequestHandler CreateInviteEmailHandler(
            IServiceScope scope,
            IEmailSender? emailSender = null,
            string? spaBaseUrl = null,
            ILogger<SendEmployeeInviteEmailRequestHandler>? logger = null
        ) => new(
            scope.ServiceProvider.GetRequiredService<ICoreDbContext>(),
            scope.ServiceProvider.GetRequiredService<IInviteTokenService>(),
            emailSender ?? fixture.EmailSender,
            scope.ServiceProvider.GetRequiredService<IEmployeeInviteEmailBuilder>(),
            spaBaseUrl is null
                ? scope.ServiceProvider.GetRequiredService<IOptions<InviteLinkOptions>>()
                : Options.Create(new InviteLinkOptions { SpaBaseUrl = spaBaseUrl }),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            logger ?? scope.ServiceProvider.GetRequiredService<ILogger<SendEmployeeInviteEmailRequestHandler>>());

        public async Task RenewInviteAsync(Guid inviteId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IInviteTokenService>();
            var invite = await dbContext.EmployeeInvites.SingleAsync(invite => invite.Id == inviteId);

            invite.Renew(tokenService.CreateSalt(), tokenService.GetExpiresAt(DateTime.UtcNow));
            await dbContext.SaveChangesAsync();
        }

        public IReadOnlyList<EmailMessage> ReadSentEmails(string recipient) => fixture.EmailSender.Messages
            .Where(message => message.To == recipient)
            .ToList();
    }

    extension(Guid inviteId)
    {
        public EmployeeInvited ToInvitedMessage(Guid companyId, string email) => new()
        {
            InviteId = inviteId,
            CompanyId = companyId,
            Email = email,
        };
    }

    extension(EmailMessage message)
    {
        public string ReadLinkToken()
        {
            var start = message.TextBody.IndexOf(InviteEmailTestData.TokenMarker, StringComparison.Ordinal)
                + InviteEmailTestData.TokenMarker.Length;
            var rest = message.TextBody[start..];

            return new string(rest.TakeWhile(character => !char.IsWhiteSpace(character)).ToArray());
        }
    }
}
