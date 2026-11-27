using FluentValidation.Results;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.UnitTests.Agents.Data;

public static class ToolTestData
{
    public static TheoryData<string, AgentRole, string[]> ToolsOfRole => new()
    {
        {
            "planner", AgentRole.Planner,
            [PlannerToolName, RejectedToolName, FailedToolName, EpisodeToolName, TimeToolName]
        },
        { "extractor", AgentRole.Extractor, [ExtractorToolName] },
    };

    public const string PlannerToolName = "echo_machine";
    public const string ExtractorToolName = "extract_only";
    public const string RejectedToolName = "always_rejected";
    public const string FailedToolName = "always_failed";
    public const string EpisodeToolName = "echo_episode";
    public const string TimeToolName = "echo_time";
    public const string ChatEmployeesToolName = "search_employees";
    public const string ChatMachinesToolName = "search_machines";
    public const string ChatNoAnswerToolName = "report_no_answer";
    public const string OutageToolName = "screen_outage";
    public const string DownstreamFailureText = "The downstream service is unavailable.";
    public const string ForeignGuidText = "11111111-1111-1111-1111-111111111111";

    public static readonly Type[] ToolTypes =
    [
        typeof(EchoRequest), typeof(ExtractRequest), typeof(RejectedRequest), typeof(FailedRequest),
        typeof(EpisodeRequest), typeof(ChatEmployeesRequest), typeof(ChatMachinesRequest),
        typeof(ChatNoAnswerRequest), typeof(OutageRequest), typeof(TimeRequest),
    ];

    public static readonly string[] AllChatToolNames =
        [ChatEmployeesToolName, ChatMachinesToolName, ChatNoAnswerToolName];
}

public sealed record EchoArguments(string Text);

public sealed record EchoResponse(Guid CompanyId, Guid MachineId, string Text);

[AgentTool(role: "planner", ToolTestData.PlannerToolName, Description = "Echoes the text.")]
[LocalTool]
public sealed record EchoRequest : IAgentToolRequest<EchoRequest, EchoArguments, EchoResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required string Text { get; init; }

    public static EchoRequest Create(EchoArguments arguments, ToolContext context) =>
        new() { CompanyId = context.CompanyId, MachineId = context.RequireMachineId(), Text = arguments.Text };
}

public sealed class EchoHandler : IRequestHandler<EchoRequest, EchoResponse>
{
    public Task<EchoResponse> HandleAsync(
        EchoRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(new EchoResponse(request.CompanyId, request.MachineId, request.Text));
}

[AgentTool(role: "extractor", ToolTestData.ExtractorToolName)]
[LocalTool]
public sealed record ExtractRequest : IAgentToolRequest<ExtractRequest, EchoArguments, EchoResponse>
{
    public static ExtractRequest Create(EchoArguments arguments, ToolContext context) => new();
}

[AgentTool(role: "planner", ToolTestData.RejectedToolName)]
[LocalTool]
public sealed record RejectedRequest : IAgentToolRequest<RejectedRequest, EchoArguments, EchoResponse>
{
    public static RejectedRequest Create(EchoArguments arguments, ToolContext context) => new();
}

public sealed class RejectedHandler : IRequestHandler<RejectedRequest, EchoResponse>
{
    public Task<EchoResponse> HandleAsync(
        RejectedRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        throw new AppValidationException(
            [new ValidationFailure(propertyName: "text", errorMessage: "Text is too long.")]);
}

[AgentTool(role: "planner", ToolTestData.FailedToolName)]
[LocalTool]
public sealed record FailedRequest : IAgentToolRequest<FailedRequest, EchoArguments, EchoResponse>
{
    public static FailedRequest Create(EchoArguments arguments, ToolContext context) => new();
}

public sealed class FailedHandler : IRequestHandler<FailedRequest, EchoResponse>
{
    public Task<EchoResponse> HandleAsync(
        FailedRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        throw new InvalidOperationException(ToolTestData.DownstreamFailureText);
}

public sealed record EpisodeArguments(Guid EpisodeId, IReadOnlyList<Guid> OtherEpisodeIds);

public sealed record EpisodeResponse(Guid Id, Guid EpisodeId, IReadOnlyList<Guid> OtherEpisodeIds);

[AgentTool(role: "planner", ToolTestData.EpisodeToolName, Description = "Echoes the episodes.")]
[LocalTool]
public sealed record EpisodeRequest : IAgentToolRequest<EpisodeRequest, EpisodeArguments, EpisodeResponse>
{
    public required Guid EpisodeId { get; init; }

    public required IReadOnlyList<Guid> OtherEpisodeIds { get; init; }

    public static EpisodeRequest Create(EpisodeArguments arguments, ToolContext context) =>
        new() { EpisodeId = arguments.EpisodeId, OtherEpisodeIds = arguments.OtherEpisodeIds };
}

public sealed record TimeArguments(DateTime From, DateTime? To);

public sealed record TimeResponse(DateTime From);

[AgentTool(role: "planner", ToolTestData.TimeToolName, Description = "Echoes the time.")]
[LocalTool]
public sealed record TimeRequest : IAgentToolRequest<TimeRequest, TimeArguments, TimeResponse>
{
    public required DateTime From { get; init; }

    public static TimeRequest Create(TimeArguments arguments, ToolContext context) => new() { From = arguments.From };
}

public sealed class TimeHandler : IRequestHandler<TimeRequest, TimeResponse>
{
    public Task<TimeResponse> HandleAsync(
        TimeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        Task.FromResult(new TimeResponse(request.From));
}

public sealed class EpisodeHandler : IRequestHandler<EpisodeRequest, EpisodeResponse>
{
    public static readonly List<Guid> Received = [];

    public Task<EpisodeResponse> HandleAsync(
        EpisodeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Received.Add(request.EpisodeId);
        Received.AddRange(request.OtherEpisodeIds);

        return Task.FromResult(new EpisodeResponse(request.EpisodeId, request.EpisodeId, request.OtherEpisodeIds));
    }
}

public sealed class TestAdvisorScopes : IAdvisorScopes
{
    private readonly IServiceProvider _provider = new ServiceCollection()
        .AddSingleton<IRequestHandler<EchoRequest, EchoResponse>, EchoHandler>()
        .AddSingleton<IRequestHandler<RejectedRequest, EchoResponse>, RejectedHandler>()
        .AddSingleton<IRequestHandler<FailedRequest, EchoResponse>, FailedHandler>()
        .AddSingleton<IRequestHandler<EpisodeRequest, EpisodeResponse>, EpisodeHandler>()
        .AddSingleton<IRequestHandler<TimeRequest, TimeResponse>, TimeHandler>()
        .AddSingleton<IRequestHandler<OutageRequest, EchoResponse>, OutageHandler>()
        .BuildServiceProvider();

    public Guid LastCompanyId { get; private set; }

    public IServiceScope Create(Guid companyId)
    {
        LastCompanyId = companyId;

        return _provider.CreateScope();
    }
}

public sealed class ChatEmployeesHandler : IRequestHandler<ChatEmployeesRequest, EchoResponse>
{
    public int Calls { get; private set; }

    public Exception? Failure { get; init; }

    public Task<EchoResponse> HandleAsync(
        ChatEmployeesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Calls++;

        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(new EchoResponse(Guid.Empty, Guid.Empty, string.Empty));
    }
}

[AgentTool(
    role: "chat",
    ToolTestData.ChatEmployeesToolName,
    Permissions = ["employees:read"],
    Activity = "people")]
public sealed record ChatEmployeesRequest : IAgentToolRequest<ChatEmployeesRequest, EchoArguments, EchoResponse>
{
    public static ChatEmployeesRequest Create(EchoArguments arguments, ToolContext context) => new();
}

[AgentTool(
    role: "chat",
    ToolTestData.ChatMachinesToolName,
    Permissions = ["machines:read", "metric-definitions:read"],
    Activity = "machines")]
public sealed record ChatMachinesRequest : IAgentToolRequest<ChatMachinesRequest, EchoArguments, EchoResponse>
{
    public static ChatMachinesRequest Create(EchoArguments arguments, ToolContext context) => new();
}

[AgentTool(role: "chat", ToolTestData.ChatNoAnswerToolName)]
public sealed record ChatNoAnswerRequest : IAgentToolRequest<ChatNoAnswerRequest, EchoArguments, EchoResponse>
{
    public static ChatNoAnswerRequest Create(EchoArguments arguments, ToolContext context) => new();
}

public sealed class TestUserScopes : IToolScopes
{
    public IServiceScope Create(Guid companyId) => new ServiceCollection().BuildServiceProvider().CreateScope();
}

[AgentTool(role: "screener", ToolTestData.OutageToolName)]
[LocalTool]
public sealed record OutageRequest : IAgentToolRequest<OutageRequest, EchoArguments, EchoResponse>
{
    public static OutageRequest Create(EchoArguments arguments, ToolContext context) => new();
}

public sealed class OutageHandler : IRequestHandler<OutageRequest, EchoResponse>
{
    public Task<EchoResponse> HandleAsync(
        OutageRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        throw new ProviderUnavailableException(ToolTestData.DownstreamFailureText, new InvalidOperationException());
}
