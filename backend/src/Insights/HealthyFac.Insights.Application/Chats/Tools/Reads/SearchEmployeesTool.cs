using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchEmployeesArguments
{
    public string? Search { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.EmployeesRead],
    Activity = "people")]
public sealed record SearchEmployeesToolRequest
    : IAgentToolRequest<SearchEmployeesToolRequest, SearchEmployeesArguments, SearchEmployeesResponse>
{
    public const string ToolName = "search_employees";

    public const string ToolDescription =
        "Searches the active employees of the company by a part of the first or last name and returns each ID "
        + "with the display name.";

    public required SearchEmployeesArguments Arguments { get; init; }

    public static SearchEmployeesToolRequest Create(SearchEmployeesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchEmployeesResponse
{
    public required IReadOnlyList<ChatEmployee> Employees { get; init; }
}

public sealed record ChatEmployee
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class SearchEmployeesArgumentsValidator : AbstractValidator<SearchEmployeesArguments>
{
    public SearchEmployeesArgumentsValidator()
    {
        RuleFor(arguments => arguments.Search!)
            .SingleLineText()
            .When(arguments => arguments.Search is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);
    }
}

public sealed class SearchEmployeesToolRequestValidator : AbstractValidator<SearchEmployeesToolRequest>
{
    public SearchEmployeesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchEmployeesArgumentsValidator());
    }
}

public sealed class SearchEmployeesToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchEmployeesToolRequest>;

public sealed class SearchEmployeesToolRequestHandler(IChatReads reads)
    : IRequestHandler<SearchEmployeesToolRequest, SearchEmployeesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<SearchEmployeesResponse> HandleAsync(
        SearchEmployeesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.SearchEmployeesAsync(request.Arguments, cancellationToken);
}
