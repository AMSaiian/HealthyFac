using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetFreeSpansArguments
{
    public required Guid EmployeeId { get; init; }

    public required string Date { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.ShiftsRead],
    Activity = "schedule")]
public sealed record GetFreeSpansToolRequest
    : IAgentToolRequest<GetFreeSpansToolRequest, GetFreeSpansArguments, GetFreeSpansResponse>
{
    public const string ToolName = "get_free_spans";

    public const string ToolDescription =
        "Reads the free spans of one employee on a date (yyyy-MM-dd): the parts of the shift that no work order "
        + "occupies.";

    public required GetFreeSpansArguments Arguments { get; init; }

    public static GetFreeSpansToolRequest Create(GetFreeSpansArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetFreeSpansResponse
{
    public required string Date { get; init; }

    public required string TimeZoneId { get; init; }

    public required IReadOnlyList<ChatScheduleBlock> Spans { get; init; }
}

public sealed class GetFreeSpansArgumentsValidator : AbstractValidator<GetFreeSpansArguments>
{
    public GetFreeSpansArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.EmployeeId);

        RuleFor(arguments => arguments.Date)
            .NotEmpty()
            .SingleLineText()
            .WithMessage(ChatToolErrors.TextHasOneLine);
    }
}

public sealed class GetFreeSpansToolRequestValidator : AbstractValidator<GetFreeSpansToolRequest>
{
    public GetFreeSpansToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetFreeSpansArgumentsValidator());
    }
}

public sealed class GetFreeSpansToolPreHandlerCheck : PassThroughPreHandlerCheck<GetFreeSpansToolRequest>;

public sealed class GetFreeSpansToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetFreeSpansToolRequest, GetFreeSpansResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetFreeSpansResponse> HandleAsync(
        GetFreeSpansToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetFreeSpansAsync(request.Arguments, cancellationToken);
}
