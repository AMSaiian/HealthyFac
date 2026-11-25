using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetMachineArguments
{
    public required Guid MachineId { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions =
    [
        ChatToolPermissions.Core.MachinesRead,
        ChatToolPermissions.Telemetry.MetricDefinitionsRead,
        ChatToolPermissions.Telemetry.StatusDefinitionsRead,
    ],
    Activity = "machines")]
public sealed record GetMachineToolRequest
    : IAgentToolRequest<GetMachineToolRequest, GetMachineArguments, GetMachineResponse>
{
    public const string ToolName = "get_machine";

    public const string ToolDescription =
        "Reads one machine by its ID: the name, the serial number, the state, the expected go-live date, the "
        + "description, its metric definitions (each with its ID, key, name, unit, and limits), and its status "
        + "definitions.";

    public required GetMachineArguments Arguments { get; init; }

    public static GetMachineToolRequest Create(GetMachineArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetMachineResponse
{
    public required ChatMachine Machine { get; init; }

    public required IReadOnlyList<ChatMetricDefinition> Metrics { get; init; }

    public required IReadOnlyList<ChatStatusDefinition> Statuses { get; init; }
}

public sealed record ChatMetricDefinition
{
    public required Guid Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string ValueKind { get; init; }

    public required string Unit { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string> States { get; init; } = [];
}

public sealed record ChatStatusDefinition
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Severity { get; init; }
}

public sealed class GetMachineArgumentsValidator : AbstractValidator<GetMachineArguments>
{
    public GetMachineArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
    }
}

public sealed class GetMachineToolRequestValidator : AbstractValidator<GetMachineToolRequest>
{
    public GetMachineToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetMachineArgumentsValidator());
    }
}

public sealed class GetMachineToolPreHandlerCheck : PassThroughPreHandlerCheck<GetMachineToolRequest>;

public sealed class GetMachineToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetMachineToolRequest, GetMachineResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetMachineResponse> HandleAsync(
        GetMachineToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetMachineAsync(request.Arguments, cancellationToken);
}
