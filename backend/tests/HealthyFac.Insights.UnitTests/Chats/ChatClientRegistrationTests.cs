using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Chats;

[Feature(TestFeatures.Chat)]
public sealed class ChatClientRegistrationTests
{
    [Theory]
    [MemberData(
        nameof(ChatClientRegistrationTestData.UserClients), MemberType = typeof(ChatClientRegistrationTestData))]
    public async Task UserClient_ForAUserFlow_SendsTheExchangedTokenOfItsAudienceAndNoActorHeader(
        string caseName,
        Func<GrpcClientFactory, Task> call,
        string audience,
        string scope
    )
    {
        using var harness = new ChatClientRegistrationHarness();

        var act = async () => await call(harness.Factory);

        await act.Should().ThrowAsync<RpcException>(caseName);
        var request = harness.Service.Request!;
        var token = request.Headers.Authorization!.Parameter!.Split(ChatClientRegistrationHarness.FormSeparator);
        token[0].Should().Be(audience);
        token[1].Split(separator: ' ').Should().Contain(scope);
        request.Headers.Contains(ActorHeaderNames.ActorId).Should().BeFalse();
        request.Headers.Contains(ActorHeaderNames.CompanyId).Should().BeFalse();
    }
}
