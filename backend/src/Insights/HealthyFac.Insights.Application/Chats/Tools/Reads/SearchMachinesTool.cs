using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchMachinesArguments
{
    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string[]? States { get; init; }

    public int? Limit { get; init; }

    public int? Offset { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.MachinesRead],
    Activity = "machines")]
public sealed record SearchMachinesToolRequest
    : IAgentToolRequest<SearchMachinesToolRequest, SearchMachinesArguments, SearchMachinesResponse>
{
    public const string ToolName = "search_machines";

    public const string ToolDescription =
        "Searches the machines of the company by the start of the name, the start of the serial number, and the "
        + "states (for example operational). Each row holds the ID, the name, the serial number, the state, and the "
        + "expected go-live date. Use limit and offset to page.";

    public required SearchMachinesArguments Arguments { get; init; }

    public static SearchMachinesToolRequest Create(SearchMachinesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchMachinesResponse
{
    public required int TotalCount { get; init; }

    public required IReadOnlyList<ChatMachine> Machines { get; init; }
}

public sealed record ChatMachine
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public required string State { get; init; }

    public required string ExpectedGoLiveOn { get; init; }

    public string? Description { get; init; }
}

public sealed class SearchMachinesArgumentsValidator : AbstractValidator<SearchMachinesArguments>
{
    public SearchMachinesArgumentsValidator()
    {
        RuleFor(arguments => arguments.Name!)
            .SingleLineText()
            .When(arguments => arguments.Name is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);

        RuleFor(arguments => arguments.SerialNumber!)
            .SingleLineText()
            .When(arguments => arguments.SerialNumber is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);

        this.AddPagingRules(arguments => arguments.Limit, arguments => arguments.Offset);

        RuleForEach(arguments => arguments.States!)
            .SingleLineText()
            .When(arguments => arguments.States is not null)
            .WithMessage(ChatToolErrors.StatesInvalid);
    }
}

public sealed class SearchMachinesToolRequestValidator : AbstractValidator<SearchMachinesToolRequest>
{
    public SearchMachinesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchMachinesArgumentsValidator());
    }
}

public sealed class SearchMachinesToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchMachinesToolRequest>;

public sealed class SearchMachinesToolRequestHandler(IChatReads reads)
    : IRequestHandler<SearchMachinesToolRequest, SearchMachinesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<SearchMachinesResponse> HandleAsync(
        SearchMachinesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.SearchMachinesAsync(request.Arguments, cancellationToken);
}
