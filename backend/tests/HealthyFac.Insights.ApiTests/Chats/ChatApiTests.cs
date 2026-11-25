using System.Net;
using System.Text.Json;
using HealthyFac.Insights.ApiTests.Cases.Data;
using HealthyFac.Insights.ApiTests.Chats.Data;
using HealthyFac.Insights.ApiTests.Hosting;
using HealthyFac.Insights.Domain.Chats.Messages;

namespace HealthyFac.Insights.ApiTests.Chats;

[Collection(InsightsApiCollection.Name)]
public sealed class ChatApiTests(InsightsApiFixture fixture)
{
    private readonly InsightsApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithReadPermission_ReturnsOnlyTheChatsOfTheUser()
    {
        var owner = Guid.CreateVersion7();
        var own = await _fixture.ArrangeChatAsync(owner);
        await _fixture.ArrangeChatAsync(Guid.CreateVersion7());

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            ChatApiTestData.SearchPath,
            _fixture.CreateChatClaims(owner, ChatApiTestData.ReadPermission),
            new { limit = 20 });

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var items = document.RootElement.GetProperty(propertyName: "items");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.GetArrayLength().Should().Be(expected: 1);
        items[index: 0].GetProperty(propertyName: "id").GetString().Should().Be(_fixture.Encrypt(own.Id));
        items[index: 0].GetProperty(propertyName: "title").GetString().Should().Be(ChatApiTestData.Question);
    }

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Post,
            ChatApiTestData.SearchPath,
            _fixture.CreateChatClaims(Guid.CreateVersion7(), ChatApiTestData.WritePermission),
            new { limit = 20 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_OfTheOwner_ReturnsTheHeader()
    {
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Get,
            ChatApiTestData.ByIdPath(_fixture.Encrypt(chat.Id)),
            _fixture.CreateChatClaims(owner, ChatApiTestData.ReadPermission));

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty(propertyName: "messageCount").GetInt32().Should().Be(expected: 2);
        document.RootElement.GetProperty(propertyName: "state").GetString().Should().Be(expected: "open");
    }

    [Fact]
    public async Task GetMessages_OfTheOwner_ReturnsTheNewestFirst()
    {
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);

        var response = await _fixture.SendCaseRequestAsync(
            HttpMethod.Get,
            ChatApiTestData.MessagesPath(_fixture.Encrypt(chat.Id)),
            _fixture.CreateChatClaims(owner, ChatApiTestData.ReadPermission));

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);
        var items = document.RootElement.GetProperty(propertyName: "items");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        items.EnumerateArray()
            .Select(item => item.GetProperty(propertyName: "role").GetString())
            .Should()
            .Equal("assistant", "user");
    }

    [Theory]
    [MemberData(nameof(ChatApiTestData.ChatEndpoints), MemberType = typeof(ChatApiTestData))]
    public async Task ChatEndpoint_WithoutItsPermission_Returns403(
        string caseName,
        string method,
        string pathTemplate,
        string permission
    )
    {
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);
        var otherPermission = permission == ChatApiTestData.ReadPermission
            ? ChatApiTestData.WritePermission
            : ChatApiTestData.ReadPermission;

        var response = await _fixture.SendCaseRequestAsync(
            new HttpMethod(method),
            ResolvePath(pathTemplate, chat),
            _fixture.CreateChatClaims(owner, otherPermission));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Theory]
    [MemberData(nameof(ChatApiTestData.ChatEndpoints), MemberType = typeof(ChatApiTestData))]
    public async Task ChatEndpoint_ForTheChatOfAnotherUser_Returns404(
        string caseName,
        string method,
        string pathTemplate,
        string permission
    )
    {
        var chat = await _fixture.ArrangeChatAsync(Guid.CreateVersion7());

        var response = await _fixture.SendCaseRequestAsync(
            new HttpMethod(method),
            ResolvePath(pathTemplate, chat),
            _fixture.CreateChatClaims(Guid.CreateVersion7(), permission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, caseName);
    }

    [Theory]
    [MemberData(nameof(ChatApiTestData.ChatEndpoints), MemberType = typeof(ChatApiTestData))]
    public async Task ChatEndpoint_WithAMalformedChatId_Returns404(
        string caseName,
        string method,
        string pathTemplate,
        string permission
    )
    {
        var response = await _fixture.SendCaseRequestAsync(
            new HttpMethod(method),
            pathTemplate
                .Replace(ChatApiTestData.ChatIdToken, ChatApiTestData.MalformedId)
                .Replace(ChatApiTestData.MessageIdToken, ChatApiTestData.MalformedId),
            _fixture.CreateChatClaims(Guid.CreateVersion7(), permission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, caseName);
    }

    [Fact]
    public async Task DeleteChat_OfTheOwner_Returns204AndTheChatIsGone()
    {
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);
        var claims = _fixture.CreateChatClaims(owner, ChatApiTestData.ReadPermission, ChatApiTestData.WritePermission);
        var path = ChatApiTestData.ByIdPath(_fixture.Encrypt(chat.Id));

        var deleted = await _fixture.SendCaseRequestAsync(HttpMethod.Delete, path, claims);
        var read = await _fixture.SendCaseRequestAsync(HttpMethod.Get, path, claims);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMessage_OfAUserMessage_Returns204AndOfAReplyReturns400()
    {
        var owner = Guid.CreateVersion7();
        var chat = await _fixture.ArrangeChatAsync(owner);
        var claims = _fixture.CreateChatClaims(owner, ChatApiTestData.WritePermission);
        var question = chat.Messages.Single(message => message.Role == ChatMessageRole.User);
        var reply = chat.Messages.Single(message => message.Role == ChatMessageRole.Assistant);

        var onReply = await _fixture.SendCaseRequestAsync(
            HttpMethod.Delete,
            ChatApiTestData.MessageByIdPath(_fixture.Encrypt(chat.Id), _fixture.Encrypt(reply.Id)),
            claims);
        var onQuestion = await _fixture.SendCaseRequestAsync(
            HttpMethod.Delete,
            ChatApiTestData.MessageByIdPath(_fixture.Encrypt(chat.Id), _fixture.Encrypt(question.Id)),
            claims);

        onReply.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        onQuestion.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private string ResolvePath(string pathTemplate, HealthyFac.Insights.Domain.Chats.Chat chat) =>
        pathTemplate
            .Replace(ChatApiTestData.ChatIdToken, _fixture.Encrypt(chat.Id))
            .Replace(
                ChatApiTestData.MessageIdToken,
                _fixture.Encrypt(chat.Messages.Single(message => message.Role == ChatMessageRole.User).Id));
}
