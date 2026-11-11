using FluentValidation;
using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Auth.Application.Locales;

public record SetMyLocaleRequest : IRequest
{
    public required Locale Locale { get; init; }
}

public class SetMyLocaleRequestValidator : AbstractValidator<SetMyLocaleRequest>
{
    public SetMyLocaleRequestValidator()
    {
        RuleFor(request => request.Locale)
            .NotNull()
            .WithMessage(LocaleErrors.LocaleRequired);
    }
}

public sealed class SetMyLocalePreHandlerCheck : PassThroughPreHandlerCheck<SetMyLocaleRequest>;

/// <summary>
/// Sets the <c>locale</c> attribute of the user who calls, so the next login page opens in that language. The user is
/// the actor of the token: a client cannot name another user.
/// </summary>
/// <remarks>
/// The Keycloak write is the only step and the last one. A cancel before it changes nothing, and a repeat writes the
/// same value, so the use case needs no finish or resume form. A failure of Keycloak answers with
/// <see cref="ServiceUnavailableException"/> (503).
/// </remarks>
/// <param name="identityAdmin">Writes the attribute.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class SetMyLocaleRequestHandler(IIdentityAdmin identityAdmin, ICurrentUserProvider currentUser)
    : RequestHandler<SetMyLocaleRequest>
{
    private readonly IIdentityAdmin _identityAdmin = identityAdmin;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override async Task HandleInternalAsync(SetMyLocaleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _identityAdmin.SetUserLocaleAsync(_currentUser.GetUserId(), request.Locale, cancellationToken);
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServiceUnavailableException(LocaleErrors.IdentityUnavailable, exception);
        }
    }
}
