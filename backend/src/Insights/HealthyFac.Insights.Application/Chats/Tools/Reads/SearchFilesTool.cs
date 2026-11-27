using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchFilesArguments
{
    public string? Search { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Document.FilesRead],
    Activity = "files")]
public sealed record SearchFilesToolRequest
    : IAgentToolRequest<SearchFilesToolRequest, SearchFilesArguments, SearchFilesResponse>
{
    public const string ToolName = "search_files";

    public const string ToolDescription =
        "Searches the files of the company by the start of the file name and returns the ID, the name, the "
        + "extension, the size, and the machine of each file.";

    public required SearchFilesArguments Arguments { get; init; }

    public static SearchFilesToolRequest Create(SearchFilesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchFilesResponse
{
    public required IReadOnlyList<ChatFile> Files { get; init; }
}

public sealed record ChatFile
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public Guid? MachineId { get; init; }
}

public sealed class SearchFilesArgumentsValidator : AbstractValidator<SearchFilesArguments>
{
    public SearchFilesArgumentsValidator()
    {
        RuleFor(arguments => arguments.Search!)
            .SingleLineText()
            .When(arguments => arguments.Search is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);
    }
}

public sealed class SearchFilesToolRequestValidator : AbstractValidator<SearchFilesToolRequest>
{
    public SearchFilesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchFilesArgumentsValidator());
    }
}

public sealed class SearchFilesToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchFilesToolRequest>;

public sealed class SearchFilesToolRequestHandler(IChatReads reads)
    : IRequestHandler<SearchFilesToolRequest, SearchFilesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<SearchFilesResponse> HandleAsync(
        SearchFilesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.SearchFilesAsync(request.Arguments, cancellationToken);
}
