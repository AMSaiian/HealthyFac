using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Agents.Tools.Writes;

/// <summary>The arguments that the model gives <c>close_case</c>.</summary>
public sealed record CloseCaseArguments
{
    public required string Reason { get; init; }
}

[AgentTool(role: "planner", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record CloseCaseToolRequest
    : IAgentToolRequest<CloseCaseToolRequest, CloseCaseArguments, CloseCaseToolResponse>
{
    public const string ToolName = "close_case";

    public const string ToolDescription =
        "Closes the case when no work is needed. Open signals that no draft and no work order cover are dismissed. The "
        + "close takes effect when the run ends.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid RunId { get; init; }

    public required string CallId { get; init; }

    public required string Reason { get; init; }

    public bool HasFailedRead { get; init; }

    public static CloseCaseToolRequest Create(CloseCaseArguments arguments, ToolContext context) =>
        new()
        {
            CompanyId = context.CompanyId,
            MachineId = context.RequireMachineId(),
            RunId = context.RunId,
            CallId = context.CurrentCallId(),
            Reason = arguments.Reason,
            HasFailedRead = context.ReadFailures.HasFailed,
        };
}

public sealed record CloseCaseToolResponse
{
    public required Guid ActionId { get; init; }

    public required string Status { get; init; }
}

public class CloseCaseToolRequestValidator : AbstractValidator<CloseCaseToolRequest>
{
    public CloseCaseToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.CallId).NotEmpty().SingleLineText();

        RuleFor(request => request.Reason)
            .NotEmpty()
            .SingleLineText()
            .MaximumLength(CaseLimits.CloseReasonMaxLength);
    }
}

public sealed class CloseCaseToolPreHandlerCheck(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CloseCaseToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CloseCaseToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Buffers a <c>closeCase</c> action. A repeated call (the same call ID) returns the action of the first call. A run
/// with a failed read never closes a case, because the evidence is incomplete: the call ends the run as unavailable.
/// </summary>
/// <param name="dbContext">Saves the action.</param>
public sealed class CloseCaseToolRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<CloseCaseToolRequest, CloseCaseToolResponse>
{
    private const string CloseWithFailedReadMessage = "A read tool failed, so the case stays open.";

    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<CloseCaseToolResponse> HandleAsync(
        CloseCaseToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (request.HasFailedRead)
        {
            throw new ProviderUnavailableException(CloseWithFailedReadMessage);
        }

        var existingId = await ActionBuffering.FindAsync(_dbContext, request.RunId, request.CallId, cancellationToken);
        var actionId = existingId
            ?? await ActionBuffering.AddAsync(
                _dbContext,
                request.RunId,
                request.CallId,
                AgentActionKind.CloseCase,
                ActionBuffering.Serialize(new CloseCasePayload { Reason = request.Reason }),
                cancellationToken);

        return new CloseCaseToolResponse
        {
            ActionId = actionId,
            Status = ProposeWorkOrderToolRequestHandler.BufferedStatus,
        };
    }
}
