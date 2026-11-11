using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.Ingest;
using HealthyFac.Insights.UnitTests.Ingest.Data;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.UnitTests.Ingest;

public sealed class ProviderResilienceTests
{
    private readonly ProviderResilience _providers = new(
        Options.Create(new IngestProcessingOptions { RetryDelayMilliseconds = 1 }),
        TimeProvider.System);

    [Theory]
    [MemberData(nameof(ProviderResilienceTestData.PermanentErrors), MemberType = typeof(ProviderResilienceTestData))]
    public async Task ExecuteAsync_WithPermanentError_FailsAtOnceWithoutRetryAndWithoutProviderText(
        Exception error
    )
    {
        var calls = 0;

        var act = () => _providers.ExecuteAsync<int>(
            _ =>
            {
                calls++;

                throw error;
            },
            TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<ProviderUnavailableException>();
        calls.Should().Be(expected: 1);
        thrown.Which.Message.Should().NotContain(ProviderResilienceTestData.ProviderText);
    }

    [Theory]
    [MemberData(nameof(ProviderResilienceTestData.TransientErrors), MemberType = typeof(ProviderResilienceTestData))]
    public async Task ExecuteAsync_WithTransientError_TriesFourTimesThenFails(Exception error)
    {
        var calls = 0;

        var act = () => _providers.ExecuteAsync<int>(
            _ =>
            {
                calls++;

                throw error;
            },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
        calls.Should().Be(ProviderResilience.MaxRetries + 1);
    }

    [Theory]
    [MemberData(nameof(ProviderResilienceTestData.PassThroughErrors), MemberType = typeof(ProviderResilienceTestData))]
    public async Task ExecuteAsync_WithOtherError_PassesItThroughOnce(Exception error)
    {
        var calls = 0;

        var act = () => _providers.ExecuteAsync<int>(
            _ =>
            {
                calls++;

                throw error;
            },
            TestContext.Current.CancellationToken);

        var thrown = await act.Should().ThrowAsync<Exception>();
        thrown.Which.Should().BeSameAs(error);
        calls.Should().Be(expected: 1);
    }
}
