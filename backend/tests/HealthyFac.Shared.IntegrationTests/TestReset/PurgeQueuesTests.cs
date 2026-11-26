using HealthyFac.Shared.IntegrationTests.TestReset.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.IntegrationTests.TestReset;

[Collection(ResetCollection.Name)]
[Feature(TestFeatures.Persistence)]
public sealed class PurgeQueuesTests(ResetFixture fixture)
{
    private readonly ResetFixture _fixture = fixture;

    [Fact]
    public async Task PurgeQueuesAsync_WithMessagesInQueue_EmptiesQueue()
    {
        await _fixture.PublishQueuedMessagesAsync();
        var queuedBefore = await _fixture.CountQueuedMessagesAsync();

        await _fixture.ResetAsync();

        var queuedAfter = await _fixture.CountQueuedMessagesAsync();

        queuedBefore.Should().Be(ResetTestData.QueuedMessageCount);
        queuedAfter.Should().Be(expected: 0);
    }
}
