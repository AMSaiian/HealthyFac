using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class EndToEndTests(PipelineFixture fixture) : IClassFixture<PipelineFixture>
{
    [Fact]
    public async Task EndToEnd_OutboxToInbox_HandlesMessageOnce()
    {
        using var scope = fixture.CreateScope(
            new ActorContext
            {
                UserId = InboxProcessorTestData.UserActorId,
                CompanyId = InboxProcessorTestData.UserCompanyId,
            });
        scope.ServiceProvider.GetRequiredService<IOutbox>()
            .Add(new TestRecorded { Text = InboxProcessorTestData.EndToEndText });
        await scope.ServiceProvider.GetRequiredService<ITestServiceDbContext>().SaveChangesAsync();

        var calls = await fixture.WaitForHandledCallsAsync(InboxProcessorTestData.EndToEndText);

        var actor = calls.Should().ContainSingle().Which.Actor!;
        actor.UserId.Should().Be(InboxProcessorTestData.UserActorId);
        actor.CompanyId.Should().Be(InboxProcessorTestData.UserCompanyId);
    }
}
