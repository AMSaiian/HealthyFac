using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(CompanyTimeZoneOutageCollection.Name)]
[Feature(TestFeatures.Chat)]
[Feature(TestFeatures.Platform)]
public sealed class CompanyTimeZoneCacheOutageTests(InsightsFixture fixture)
{
    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task ReadTimeZoneId_WhenRedisIsStopped_ReturnsTheZoneFromCore()
    {
        await _fixture.StopRedisAsync();

        using var scope = _fixture.CreateActorScope(
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = ChatTestData.OwnerId,
                CompanyId = ChatTestData.CompanyId,
            });

        var reader = scope.ServiceProvider.GetRequiredService<ICompanyTimeZoneReader>();

        var zoneId = await reader.ReadTimeZoneIdAsync(CancellationToken.None);

        zoneId.Should().Be(FakeCoreCompanies.ZoneId);
        _fixture.CoreCompanies.CallCount.Should().Be(expected: 1);
    }
}
