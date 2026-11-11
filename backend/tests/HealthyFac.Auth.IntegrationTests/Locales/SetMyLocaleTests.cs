using HealthyFac.Auth.Application.Locales;
using HealthyFac.Auth.IntegrationTests.Identities.Data;
using HealthyFac.Auth.IntegrationTests.Locales.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Auth.IntegrationTests.Locales;

public sealed class SetMyLocaleTests
{
    [Fact]
    public async Task Handle_WithSupportedLocale_SetsLocaleOfActorOnly()
    {
        var actorId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();
        var identityAdmin = new FakeIdentityAdmin();
        var handler = new SetMyLocaleRequestHandler(identityAdmin, new FixedCurrentUserProvider(actorId));

        await handler.HandleAsync(
            new SetMyLocaleRequest { Locale = Locale.Ukrainian },
            TestContext.Current.CancellationToken);

        identityAdmin.LocaleOf(actorId).Should().Be(LocaleTestData.Ukrainian);
        identityAdmin.LocaleOf(otherId).Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenIdentityProviderIsDown_ThrowsServiceUnavailable()
    {
        var identityAdmin = new FakeIdentityAdmin { IsUnavailable = true };
        var handler = new SetMyLocaleRequestHandler(identityAdmin, new FixedCurrentUserProvider(Guid.CreateVersion7()));
        var act = () => handler.HandleAsync(
            new SetMyLocaleRequest { Locale = Locale.Ukrainian },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ServiceUnavailableException>().WithMessage(LocaleErrors.IdentityUnavailable);
    }
}
