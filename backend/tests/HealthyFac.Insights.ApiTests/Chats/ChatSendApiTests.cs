using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HealthyFac.Insights.ApiTests.Chats.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Testing.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.ApiTests.Chats;

[Collection(InsightsApiCollection.Name)]
[Feature(TestFeatures.Chat)]
public sealed class ChatSendApiTests(InsightsApiFixture fixture)
{
    private const string BearerScheme = "Bearer";
    private const int PollAttempts = 100;
    private const int PollDelayMilliseconds = 100;

    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithAText_Returns201WithTheChatAndBothMessages()
    {
        var fakes = new ChatSendApiFakes();
        await using var factory = _fixture.CreateFactory(fakes.Register);
        var owner = Guid.CreateVersion7();

        var response = await SendAsync(
            factory, HttpMethod.Post, ChatApiTestData.BasePath, owner, new { text = ChatApiTestData.Question });

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var chat = document.RootElement.GetProperty(propertyName: "chat");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith(chat.GetProperty(propertyName: "id").GetString()!);
        chat.GetProperty(propertyName: "title").GetString().Should().Be(ChatApiTestData.Question);

        document.RootElement.GetProperty(propertyName: "user")
            .GetProperty(propertyName: "role")
            .GetString()
            .Should()
            .Be(expected: "user");

        var assistant = document.RootElement.GetProperty(propertyName: "assistant");
        assistant.GetProperty(propertyName: "state").GetString().Should().Be(expected: "completed");
        assistant.GetProperty(propertyName: "text").GetString().Should().Be(ChatApiTestData.Answer);
    }

    [Fact]
    public async Task Send_ToTheChatOfTheUser_Returns200WithTheNewMessages()
    {
        var fakes = new ChatSendApiFakes();
        await using var factory = _fixture.CreateFactory(fakes.Register);
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);

        var response = await SendAsync(
            factory,
            HttpMethod.Post,
            ChatApiTestData.MessagesBasePath(_fixture.Encrypt(chat.Id)),
            owner,
            new { text = ChatApiTestData.Question });

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        document.RootElement.GetProperty(propertyName: "user")
            .GetProperty(propertyName: "sequence")
            .GetInt32()
            .Should()
            .Be(expected: 3);

        document.RootElement.GetProperty(propertyName: "assistant")
            .GetProperty(propertyName: "state")
            .GetString()
            .Should()
            .Be(expected: "completed");
    }

    [Fact]
    public async Task Send_WhenTheRequestIsAborted_StoresCancelledAndPushesTheEnd()
    {
        var fakes = new ChatSendApiFakes { Model = { IsBlocking = true } };
        await using var factory = _fixture.CreateFactory(fakes.Register);
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);
        using var abort = new CancellationTokenSource();

        var sending = SendAsync(
            factory,
            HttpMethod.Post,
            ChatApiTestData.MessagesBasePath(_fixture.Encrypt(chat.Id)),
            owner,
            new { text = ChatApiTestData.Question },
            abort.Token);

        await fakes.Model.Started.WaitAsync(TimeSpan.FromSeconds(seconds: 20), TestContext.Current.CancellationToken);
        await abort.CancelAsync();
        var act = () => sending;
        await act.Should().ThrowAsync<OperationCanceledException>();

        var stored = await WaitForCancelledReplyAsync(chat.Id);
        var ends = fakes.Queue.Items.Select(item => item.Push.ChatTurnEnd).Where(end => end is not null).ToList();
        stored.Should().NotBeNull();
        stored.Text.Should().Be(ChatSendApiFakes.PartialText);
        ends.Should().ContainSingle().Which!.State.Should().Be(expected: "cancelled");
    }

    [Fact]
    public async Task Send_WithAMalformedChatId_Returns404()
    {
        var response = await SendAsync(
            factory: null,
            HttpMethod.Post,
            ChatApiTestData.MessagesBasePath(ChatApiTestData.MalformedId),
            Guid.CreateVersion7(),
            new { text = ChatApiTestData.Question });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Send_WithoutWritePermission_Returns403()
    {
        var response = await SendAsync(
            factory: null,
            HttpMethod.Post,
            ChatApiTestData.BasePath,
            Guid.CreateVersion7(),
            new { text = ChatApiTestData.Question },
            permission: ChatApiTestData.ReadPermission);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAnUnknownLinkKind_Returns400AndStoresNothing()
    {
        var owner = Guid.CreateVersion7();

        var response = await SendAsync(
            factory: null,
            HttpMethod.Post,
            ChatApiTestData.BasePath,
            owner,
            new
            {
                text = ChatApiTestData.Question,
                links = new[] { new { kind = "unknown", entityId = _fixture.Encrypt(Guid.CreateVersion7()) } },
            });

        var count = await CountChatsAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        count.Should().Be(expected: 0);
    }

    [Fact]
    public async Task Create_WithNullLinks_Returns201WithoutLinks()
    {
        var fakes = new ChatSendApiFakes();
        await using var factory = _fixture.CreateFactory(fakes.Register);

        var response = await SendAsync(
            factory,
            HttpMethod.Post,
            ChatApiTestData.BasePath,
            Guid.CreateVersion7(),
            new { text = ChatApiTestData.Question, links = (object?)null });

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        document.RootElement.GetProperty(propertyName: "user")
            .GetProperty(propertyName: "links")
            .GetArrayLength()
            .Should()
            .Be(expected: 0);
    }

    [Theory]
    [MemberData(nameof(ChatApiTestData.BadLinkIds), MemberType = typeof(ChatApiTestData))]
    public async Task Create_WithAnUnreadableLinkId_Returns400AndStoresNothing(string caseName, string entityId)
    {
        var response = await SendAsync(
            factory: null,
            HttpMethod.Post,
            ChatApiTestData.BasePath,
            Guid.CreateVersion7(),
            new
            {
                text = ChatApiTestData.Question,
                links = new[] { new { kind = "machine", entityId } },
            });

        var count = await CountChatsAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
        count.Should().Be(expected: 0);
    }

    private async Task<HttpResponseMessage> SendAsync(
        InsightsApiFactory? factory,
        HttpMethod method,
        string path,
        Guid userId,
        object body,
        CancellationToken? cancellationToken = null,
        string permission = ChatApiTestData.WritePermission
    )
    {
        var ownFactory = factory is null ? _fixture.CreateFactory(new ChatSendApiFakes().Register) : null;

        try
        {
            using var client = (factory ?? ownFactory!).CreateClient();
            using var request = new HttpRequestMessage(method, path);

            request.Headers.Authorization = new AuthenticationHeaderValue(
                BearerScheme,
                _fixture.Tokens.CreateToken(_fixture.CreateChatClaims(userId, permission)));

            request.Content = JsonContent.Create(body);

            return await client.SendAsync(request, cancellationToken ?? TestContext.Current.CancellationToken);
        }
        finally
        {
            if (ownFactory is not null)
            {
                await ownFactory.DisposeAsync();
            }
        }
    }

    private async Task<int> CountChatsAsync()
    {
        using var scope = _fixture.ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

        return await dbContext.Chats.CountAsync();
    }

    private async Task<ChatMessage?> WaitForCancelledReplyAsync(Guid chatId)
    {
        for (var attempt = 0; attempt < PollAttempts; attempt++)
        {
            using var scope = _fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IInsightsDbContext>();

            var reply = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(message => message.ChatId == chatId && message.State == ChatMessageState.Cancelled)
                .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

            if (reply is not null)
            {
                return reply;
            }

            await Task.Delay(PollDelayMilliseconds, TestContext.Current.CancellationToken);
        }

        return null;
    }
}
