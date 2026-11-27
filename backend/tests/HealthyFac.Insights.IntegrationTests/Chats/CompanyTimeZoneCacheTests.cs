using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.IntegrationTests.Database;
using HealthyFac.Insights.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(InsightsCollection.Name)]
[Feature(TestFeatures.Chat)]
[Feature(TestFeatures.Platform)]
public sealed class CompanyTimeZoneCacheTests(InsightsFixture fixture)
{
    private const string KeyPrefix = "insights:company-zone:";
    private const int MinTimeToLiveSeconds = 86400;
    private const int MaxTimeToLiveSeconds = 90000;

    private readonly InsightsFixture _fixture = fixture;

    [Fact]
    public async Task ReadTimeZoneId_TwiceForOneCompany_CallsCoreOnce()
    {
        var first = await ReadZoneAsync(ChatTestData.CompanyId);

        var second = await ReadZoneAsync(ChatTestData.CompanyId);

        first.Should().Be(FakeCoreCompanies.ZoneId);
        second.Should().Be(FakeCoreCompanies.ZoneId);
        _fixture.CoreCompanies.CallCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task ReadTimeZoneId_AfterTheFirstRead_StoresTheEntryWithTimeToLiveAndJitter()
    {
        await ReadZoneAsync(ChatTestData.CompanyId);

        var timeToLive = await _fixture.GetSingleton<IConnectionMultiplexer>()
            .GetDatabase()
            .KeyTimeToLiveAsync($"{KeyPrefix}{ChatTestData.CompanyId}");

        timeToLive.Should().NotBeNull();
        timeToLive.Value.TotalSeconds.Should().BeInRange(MinTimeToLiveSeconds - 1, MaxTimeToLiveSeconds);
    }

    [Fact]
    public async Task ReadTimeZoneId_ForTwoCompanies_StoresTwoEntries()
    {
        await ReadZoneAsync(ChatTestData.CompanyId);
        await ReadZoneAsync(ChatTestData.OtherCompanyId);

        var database = _fixture.GetSingleton<IConnectionMultiplexer>().GetDatabase();
        var isFirstPresent = await database.KeyExistsAsync($"{KeyPrefix}{ChatTestData.CompanyId}");
        var isSecondPresent = await database.KeyExistsAsync($"{KeyPrefix}{ChatTestData.OtherCompanyId}");

        isFirstPresent.Should().BeTrue();
        isSecondPresent.Should().BeTrue();
        _fixture.CoreCompanies.CallCount.Should().Be(expected: 2);
    }

    private async Task<string> ReadZoneAsync(Guid companyId)
    {
        using var scope = _fixture.CreateActorScope(
            new ActorContext { Kind = ActorKind.User, UserId = ChatTestData.OwnerId, CompanyId = companyId });

        var reader = scope.ServiceProvider.GetRequiredService<ICompanyTimeZoneReader>();

        return await reader.ReadTimeZoneIdAsync(CancellationToken.None);
    }
}
