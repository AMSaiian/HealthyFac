using FluentValidation.Results;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.UnitTests.Agents.Data;

public static class ToolTestData
{
    public const string PlannerToolName = "echo_machine";
    public const string ExtractorToolName = "extract_only";
    public const string RejectedToolName = "always_rejected";
    public const string FailedToolName = "always_failed";
    public const string EpisodeToolName = "echo_episode";
    public const string DownstreamFailureText = "The downstream service is unavailable.";
    public const string ForeignGuidText = "11111111-1111-1111-1111-111111111111";

    public static readonly Type[] ToolTypes =
    [
        typeof(EchoRequest), typeof(ExtractRequest), typeof(RejectedRequest), typeof(FailedRequest),
        typeof(EpisodeRequest),
    ];
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
        .BuildServiceProvider();

    public Guid LastCompanyId { get; private set; }

    public IServiceScope Create(Guid companyId)
    {
        LastCompanyId = companyId;

        return _provider.CreateScope();
    }
}
