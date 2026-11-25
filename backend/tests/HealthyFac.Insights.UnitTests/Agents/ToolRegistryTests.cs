using System.Text.Json;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Infrastructure.Agents.Tools;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HealthyFac.Insights.UnitTests.Agents;

[Feature(TestFeatures.Cases)]
public sealed class ToolRegistryTests
{
    private static readonly Guid CompanyId = Guid.Parse(input: "22222222-2222-2222-2222-222222222222");
    private static readonly Guid MachineId = Guid.Parse(input: "33333333-3333-3333-3333-333333333333");

    private readonly TestAdvisorScopes _scopes = new();
    private readonly List<AgentToolCall> _rows = [];
    private readonly FakeLogger _logger = new();

    [Theory]
    [MemberData(nameof(ToolTestData.ToolsOfRole), MemberType = typeof(ToolTestData))]
    public void For_WithARole_ReturnsOnlyTheToolsOfThatRole(
        string caseName,
        AgentRole role,
        string[] expectedNames
    )
    {
        var functions = CreateRegistry().For(role, CreateContext());

        functions.Select(function => function.Name).Should().BeEquivalentTo(expectedNames, caseName);
    }

    [Fact]
    public void For_WithTheRoleChatAndATokenWithoutEmployeesRead_LeavesOutTheEmployeeTool()
    {
        var functions = CreateRegistry()
            .For(AgentRole.Chat, CreateContext(), ["machines:read", "metric-definitions:read"], new TestUserScopes());

        functions.Select(function => function.Name)
            .Should()
            .BeEquivalentTo(ToolTestData.ChatMachinesToolName, ToolTestData.ChatNoAnswerToolName);
    }

    [Fact]
    public void For_WithTheRoleChatAndATokenWithOnePermissionOfAToolOnly_LeavesOutThatTool()
    {
        var functions = CreateRegistry()
            .For(AgentRole.Chat, CreateContext(), ["machines:read"], new TestUserScopes());

        functions.Select(function => function.Name).Should().BeEquivalentTo(ToolTestData.ChatNoAnswerToolName);
    }

    [Fact]
    public void For_WithTheRoleChatAndATokenWithAllPermissions_ReturnsEveryChatTool()
    {
        var functions = CreateRegistry()
            .For(
                AgentRole.Chat,
                CreateContext(),
                ["employees:read", "machines:read", "metric-definitions:read"],
                new TestUserScopes());

        functions.Select(function => function.Name).Should().BeEquivalentTo(ToolTestData.AllChatToolNames);
    }

    [Fact]
    public void For_WithTheRoleChatAndNoScopes_Throws()
    {
        var act = () => CreateRegistry().For(AgentRole.Chat, CreateContext(), ["employees:read"]);

        act.Should().Throw<InvalidOperationException>().WithMessage(AgentMessages.ChatToolsNeedUserScopes);
    }

    [Fact]
    public void For_WithTheRoleChatAndTheAdvisorScopes_Throws()
    {
        var act = () => CreateRegistry().For(AgentRole.Chat, CreateContext(), ["employees:read"], _scopes);

        act.Should().Throw<InvalidOperationException>().WithMessage(AgentMessages.ChatToolsNeedUserScopes);
    }

    [Fact]
    public async Task Invoke_OfAnOtherRoleWhenTheServiceIsDown_EndsTheLoopAndThrowsTheOutage()
    {
        var function = CreateRegistry()
            .For(AgentRole.Screener, CreateContext())
            .Single(candidate => candidate.Name == ToolTestData.OutageToolName);

        var act = async () => await function.InvokeAsync(
            new AIFunctionArguments { [key: "text"] = "x" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
        _rows.Single().Outcome.Should().Be(ToolCallOutcome.Failed);
    }

    [Fact]
    public async Task Invoke_OfAChatTool_RunsInTheRequestScopeOfTheUser()
    {
        var handler = new ChatEmployeesHandler();
        var provider = new ServiceCollection()
            .AddSingleton<IRequestHandler<ChatEmployeesRequest, EchoResponse>>(handler)
            .BuildServiceProvider();
        var function = CreateRegistry()
            .For(AgentRole.Chat, CreateContext(), ["employees:read"], new ChatToolScopes(provider))
            .Single(candidate => candidate.Name == ToolTestData.ChatEmployeesToolName);

        await function.InvokeAsync(
            new AIFunctionArguments { [key: "text"] = "x" },
            TestContext.Current.CancellationToken);

        handler.Calls.Should().Be(expected: 1);
        _scopes.LastCompanyId.Should().BeEmpty();
        _rows.Single().Outcome.Should().Be(ToolCallOutcome.Succeeded);
        provider.GetService<IRequestHandler<ChatEmployeesRequest, EchoResponse>>().Should().NotBeNull();
    }

    [Fact]
    public void ChatToolScopes_Create_ReturnsTheRequestProviderAndLeavesItOpenOnDispose()
    {
        var provider = new ServiceCollection().AddSingleton(new object()).BuildServiceProvider();

        var scope = new ChatToolScopes(provider).Create(CompanyId);
        scope.Dispose();

        scope.ServiceProvider.Should().BeSameAs(provider);
        provider.GetService<object>().Should().NotBeNull();
    }

    [Fact]
    public async Task Invoke_WithACompanyAndMachineInTheArguments_UsesTheValuesOfTheContext()
    {
        var function = FindFunction(ToolTestData.PlannerToolName);

        var arguments = new AIFunctionArguments
        {
            ["text"] = "hello",
            ["companyId"] = ToolTestData.ForeignGuidText,
            ["machineId"] = ToolTestData.ForeignGuidText,
        };

        var result = await function.InvokeAsync(arguments, TestContext.Current.CancellationToken);

        var row = _rows.Single();
        var text = result!.ToString()!;

        text.Should()
            .StartWith(expected: "<tool_result>")
            .And.Contain(new PrefixIdEncryptor().Encrypt(CompanyId))
            .And.Contain(expected: "hello");

        text.Should().NotContain(ToolTestData.ForeignGuidText);
        _scopes.LastCompanyId.Should().Be(CompanyId);
        row.Outcome.Should().Be(ToolCallOutcome.Succeeded);
        row.Sequence.Should().Be(expected: 1);
        row.ToolName.Should().Be(ToolTestData.PlannerToolName);
    }

    [Fact]
    public async Task Invoke_WithEncryptedEpisodeIds_BindsTheRawGuidsAndReturnsEncryptedIds()
    {
        var encryptor = new PrefixIdEncryptor();
        var first = Guid.Parse(input: "44444444-4444-4444-4444-444444444444");
        var second = Guid.Parse(input: "55555555-5555-5555-5555-555555555555");
        EpisodeHandler.Received.Clear();
        var function = FindFunction(ToolTestData.EpisodeToolName);

        var arguments = new AIFunctionArguments
        {
            ["episodeId"] = encryptor.Encrypt(first),
            ["otherEpisodeIds"] = new[] { encryptor.Encrypt(second) },
        };

        var result = await function.InvokeAsync(arguments, TestContext.Current.CancellationToken);

        EpisodeHandler.Received.Should().Equal(first, second);
        var text = result!.ToString()!;
        text.Should().Contain(encryptor.Encrypt(first)).And.Contain(encryptor.Encrypt(second));
        text.Should().NotContain(first.ToString()).And.NotContain(second.ToString());
        var row = _rows.Single();
        row.Result.Should().NotContain(first.ToString()).And.Contain(encryptor.Encrypt(first));
        row.Arguments.Should().NotContain(first.ToString());
    }

    [Fact]
    public void JsonSchema_OfAToolWithGuidArguments_HasPlainStringsWithoutTheUuidFormat()
    {
        var function = FindFunction(ToolTestData.EpisodeToolName);

        function.JsonSchema.GetRawText().Should().NotContain(unexpected: "uuid");
    }

    [Fact]
    public async Task Invoke_WhenTheUseCaseRejectsTheRequest_ReturnsTheMessageWithTheOutcomeRejected()
    {
        var function = FindFunction(ToolTestData.RejectedToolName);

        var result = await function.InvokeAsync(
            new AIFunctionArguments { ["text"] = "x" },
            TestContext.Current.CancellationToken);

        var row = _rows.Single();
        result!.ToString().Should().Contain(expected: "Text is too long.");
        row.Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Invoke_WithArgumentsOfTheWrongType_ReturnsTheOutcomeRejected()
    {
        var function = FindFunction(ToolTestData.PlannerToolName);

        await function.InvokeAsync(
            new AIFunctionArguments { ["text"] = JsonDocument.Parse(json: "[1]").RootElement },
            TestContext.Current.CancellationToken);

        _rows.Single().Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task Invoke_WhenTheUseCaseFails_LogsAWarningWithTheExceptionAndReturnsAFixedText()
    {
        var function = FindFunction(ToolTestData.FailedToolName);

        var result = await function.InvokeAsync(
            new AIFunctionArguments { ["text"] = "x" },
            TestContext.Current.CancellationToken);

        var entry = _logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);

        entry.Exception.Should()
            .BeOfType<InvalidOperationException>()
            .Which.Message
            .Should()
            .Be(ToolTestData.DownstreamFailureText);

        result!.ToString().Should().NotContain(ToolTestData.DownstreamFailureText);
        _rows.Single().Outcome.Should().Be(ToolCallOutcome.Failed);
    }

    [Fact]
    public async Task Invoke_WhenTheUseCaseRejectsTheRequest_LogsNoWarning()
    {
        var function = FindFunction(ToolTestData.RejectedToolName);

        await function.InvokeAsync(
            new AIFunctionArguments { ["text"] = "x" },
            TestContext.Current.CancellationToken);

        _logger.Collector.GetSnapshot().Should().BeEmpty();
    }

    private ToolRegistry CreateRegistry() => new(
        _scopes, ToolTestData.ToolTypes, _logger, new AgentJson(new PrefixIdEncryptor()));

    private ToolContext CreateContext() =>
        new()
        {
            CompanyId = CompanyId,
            MachineId = MachineId,
            RunId = Guid.NewGuid(),
            Recorder = new ToolCallRecorder(
                Guid.NewGuid(),
                (createRow, _) =>
                {
                    _rows.Add(createRow(_rows.Count + 1));

                    return Task.CompletedTask;
                }),
            CaseId = Guid.NewGuid(),
        };

    private AIFunction FindFunction(string name) =>
        CreateRegistry().For(AgentRole.Planner, CreateContext()).Single(function => function.Name == name);
}
