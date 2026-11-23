using HealthyFac.Core.Application.Companies;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.Relay.Data;
using HealthyFac.Seeder.IntegrationTests.ServiceHosts.Data;
using HealthyFac.Seeder.ServiceHosts;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Telemetry.Messaging.MetricUnits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using SeederHosts = HealthyFac.Seeder.ServiceHosts.ServiceHosts;

namespace HealthyFac.Seeder.IntegrationTests.ServiceHosts;

[Collection(SeederCollection.Name)]
public sealed class ServiceHostsTests(SeederFixture fixture)
{
    private readonly SeederFixture _fixture = fixture;

    [Fact]
    public async Task InitializeAsync_OnEmptyDatabases_CreatesTheSchemasAndTheRequiredData()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        var roleCount = await _fixture.CountRolesAsync();
        var unitCount = await _fixture.CountMeasurementUnitsAsync();

        roleCount.Should().Be(ServiceHostsTestData.RoleCount);
        unitCount.Should().BePositive();
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ChangesNothing()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var roleCountBefore = await _fixture.CountRolesAsync();
        var unitCountBefore = await _fixture.CountMeasurementUnitsAsync();

        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        var roleCountAfter = await _fixture.CountRolesAsync();
        var unitCountAfter = await _fixture.CountMeasurementUnitsAsync();
        roleCountAfter.Should().Be(roleCountBefore);
        unitCountAfter.Should().Be(unitCountBefore);
    }

    [Fact]
    public async Task SendAsync_WithAnInvalidRequest_ThrowsTheValidationException()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);
        var request = new CreateMachineRequest
        {
            Name = string.Empty,
            SerialNumber = string.Empty,
            CompanyId = ServiceHostsTestData.CompanyId,
            ExpectedGoLiveOn = new DateOnly(year: 2027, month: 1, day: 1),
        };

        var act = () => _fixture.Hosts.Core.SendAsync<CreateMachineRequest, CreateMachineResponse>(
            ServiceHostsTestData.Actor,
            request,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task SendAsync_WithAValidRequest_RunsTheHandler()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        var act = () => _fixture.Hosts.Core.SendAsync<GetCurrentCompanyRequest, GetCurrentCompanyResponse>(
            ServiceHostsTestData.Actor,
            new GetCurrentCompanyRequest(),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Create_WithoutARequiredKey_FailsWithTheServiceAndTheKey()
    {
        var settings = _fixture.CreateSettings();
        settings.Remove(ServiceHostsTestData.RequiredKey);

        var act = () => SeederHosts.Create(
            SeederFixture.CreateConfiguration(settings),
            new SeedClock(),
            AppContext.BaseDirectory);

        act.Should().Throw<InvalidOperationException>().WithMessage(ServiceHostsTestData.RequiredKeyMessage);
    }

    [Fact]
    public void Create_ForAllServices_ReplacesTheRealtimeQueueWithTheDiscardingQueue()
    {
        using var scope = _fixture.Hosts.Core.CreateScope(ServiceHostsTestData.Actor);

        var queue = scope.ServiceProvider.GetRequiredService<IBackgroundQueue<RealtimePush>>();

        queue.Should().BeOfType<DiscardingRealtimeQueue>();
    }

    [Fact]
    public async Task Validate_WithAnInvalidRequest_ReturnsOneTextForEachFailure()
    {
        var request = new CreateMachineRequest
        {
            Name = string.Empty,
            SerialNumber = "PR-0001",
            CompanyId = ServiceHostsTestData.CompanyId,
            ExpectedGoLiveOn = new DateOnly(year: 2027, month: 1, day: 1),
        };

        var failures = await _fixture.Hosts.Core.ValidateAsync(request, TestContext.Current.CancellationToken);

        failures.Should()
            .NotBeEmpty()
            .And.OnlyContain(text => text.StartsWith("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_WithAValidRequest_ReturnsNoFailure()
    {
        var request = new CreateMachineRequest
        {
            Name = "Press 1",
            SerialNumber = "PR-0001",
            CompanyId = ServiceHostsTestData.CompanyId,
            ExpectedGoLiveOn = new DateOnly(year: 2027, month: 1, day: 1),
        };

        var failures = await _fixture.Hosts.Core.ValidateAsync(request, TestContext.Current.CancellationToken);

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task DrainInboxAsync_OnEmptyInboxesWithAnUnreachableBroker_CompletesInEveryHost()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        var act = () => Task.WhenAll(
            _fixture.Hosts.All.Select(host => host.DrainInboxAsync(TestContext.Current.CancellationToken)));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Create_WithoutServicesKeysForTheFileValues_ReadsThemFromTheDevelopmentSettingsFile()
    {
        var fileValue = ServiceHostsTestData.ReadDevelopmentSetting(
            service: "document", section: "FileStorage", key: "PublicBlobBaseUrl");

        var blobBaseUrl = _fixture.Hosts.Document.ReadOption(options => options.PublicBlobBaseUrl);

        blobBaseUrl.Should().Be(fileValue);
    }

    [Fact]
    public async Task Create_WithAServicesKeyForAFileValue_UsesTheKey()
    {
        var settings = _fixture.CreateSettings();
        settings[ServiceHostsTestData.BlobUrlKey] = ServiceHostsTestData.OverrideBlobUrl;
        await using var hosts = SeederHosts.Create(
            SeederFixture.CreateConfiguration(settings),
            new SeedClock(),
            AppContext.BaseDirectory);

        var blobBaseUrl = hosts.Document.ReadOption(options => options.PublicBlobBaseUrl);

        blobBaseUrl.Should().Be(ServiceHostsTestData.OverrideBlobUrl);
    }

    [Fact]
    public async Task InitializeAsync_AtTheInitializationMoment_RelaysTheUnitMessageWithThatTime()
    {
        var moment = await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        await _fixture.Hosts.CreateRelay().RunAsync(eventItem: "initialization", TestContext.Current.CancellationToken);

        var telemetryOutbox = await _fixture.LoadOutboxAsync(_fixture.Hosts.Telemetry);
        var insightsInbox = await _fixture.LoadInboxAsync(_fixture.Hosts.Insights);
        var sent = telemetryOutbox.Single(message => message.Type == MetricUnitsChanged.MessageType);
        sent.ProcessedOn.Should().NotBeNull();
        sent.OccurredOn.Should().BeCloseTo(moment.UtcDateTime, ServiceHostsTestData.ClockTolerance);
        insightsInbox.Single(message => message.Id == sent.Id).ProcessedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTokenAsync_AfterTheSeedClockJumps30Days_ReturnsTheCachedToken()
    {
        var moment = _fixture.EnterNextMoment(item: "token");
        var first = await _fixture.GetAuthTokenAsync(TestContext.Current.CancellationToken);
        _fixture.Clock.Enter(moment.Add(ServiceHostsTestData.ClockJump), item: "token-later");

        var second = await _fixture.GetAuthTokenAsync(TestContext.Current.CancellationToken);

        second.Should().Be(first);
    }

    [Fact]
    public async Task GetTokenAsync_AfterTheRealLifetimeEnded_FetchesANewToken()
    {
        _fixture.EnterNextMoment(item: "token");
        await _fixture.SetAuthClientTokenLifespanAsync(ServiceHostsTestData.ShortLifespanSeconds);
        try
        {
            var first = await _fixture.GetAuthTokenAsync(TestContext.Current.CancellationToken);

            var second = await _fixture.GetAuthTokenAsync(TestContext.Current.CancellationToken);

            second.Should().NotBe(first);
        }
        finally
        {
            await _fixture.SetAuthClientTokenLifespanAsync(seconds: null);
        }
    }

    [Fact]
    public void GetUtcNow_OnTheAuthHostAfterEnter_ReturnsTheSeedMoment()
    {
        var moment = _fixture.EnterNextMoment(item: "clock");

        var read = _fixture.Hosts.Auth.GetService<TimeProvider>().GetUtcNow();

        read.Should().Be(moment);
    }

    [Fact]
    public async Task GetServices_OnEveryHost_ReturnsNoHostedService()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        var hostedServiceCounts = _fixture.Hosts.All.Select(host =>
            {
                using var scope = host.CreateScope(ServiceHostsTestData.Actor);

                return scope.ServiceProvider.GetServices<IHostedService>().Count();
            })
            .ToList();

        hostedServiceCounts.Should().AllBeEquivalentTo(expectation: 0);
    }

    [Fact]
    public async Task Publish_OnTheBusOfEveryHost_ThrowsTheNoMessageBusError()
    {
        await _fixture.InitializeHostsAsync(TestContext.Current.CancellationToken);

        foreach (var host in _fixture.Hosts.All)
        {
            using var scope = host.CreateScope(ServiceHostsTestData.Actor);
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();

            var act = () => bus.Publish(new object());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(ServiceHostErrors.NoMessageBus);
        }
    }
}
