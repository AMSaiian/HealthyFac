using FluentValidation;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Documents;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchManualArguments
{
    public required Guid MachineId { get; init; }

    public required string Query { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Insights.IngestRead],
    Activity = "manuals")]
public sealed record SearchManualToolRequest
    : IAgentToolRequest<SearchManualToolRequest, SearchManualArguments, SearchManualResponse>
{
    public const string ToolName = "search_manual_passages";

    public const string ToolDescription =
        "Searches the documents of one machine (manuals, procedures) and returns the passages that are nearest to "
        + "the query, each with its location. Give the machine by its ID.";

    public required SearchManualArguments Arguments { get; init; }

    public static SearchManualToolRequest Create(SearchManualArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchManualResponse
{
    public required IReadOnlyList<ChatManualPassage> Passages { get; init; }
}

public sealed record ChatManualPassage
{
    public string? Location { get; init; }

    /// <summary>The text of the chunk inside an escaped data block.</summary>
    public required string Text { get; init; }
}

public sealed class SearchManualArgumentsValidator : AbstractValidator<SearchManualArguments>
{
    public SearchManualArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);

        RuleFor(arguments => arguments.Query)
            .NotEmpty()
            .SingleLineText()
            .WithMessage(AgentMessages.QueryRequired);
    }
}

public sealed class SearchManualToolRequestValidator : AbstractValidator<SearchManualToolRequest>
{
    public SearchManualToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchManualArgumentsValidator());
    }
}

public sealed class SearchManualToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchManualToolRequest>;

/// <summary>
/// Embeds the query and returns the nearest chunks of the documents that are tied to the machine inside the company
/// of the user. The number of chunks is the option <see cref="AgentOptions.ManualSearchChunks"/>. Each chunk text goes
/// into an escaped data block, because a manual is data and never an instruction.
/// </summary>
/// <param name="embedder">Embeds the query.</param>
/// <param name="search">Finds the nearest chunks.</param>
/// <param name="options">Holds the number of chunks.</param>
/// <param name="currentUser">Provides the company of the user.</param>
public sealed class SearchManualToolRequestHandler(
    ChunkEmbedder embedder,
    IManualSearch search,
    IOptions<AgentOptions> options,
    ICurrentUserProvider currentUser
) : IRequestHandler<SearchManualToolRequest, SearchManualResponse>
{
    public const string ChunkTag = "manual_chunk";

    private readonly ChunkEmbedder _embedder = embedder;
    private readonly IManualSearch _search = search;
    private readonly AgentOptions _options = options.Value;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<SearchManualResponse> HandleAsync(
        SearchManualToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var vectors = await _embedder.EmbedAsync([request.Arguments.Query], cancellationToken);
        var chunks = await ManualChunkFinder.FindAsync(
            _search,
            _currentUser.GetCompanyId(),
            request.Arguments.MachineId,
            vectors[index: 0],
            _options.ManualSearchChunks,
            cancellationToken);

        return new SearchManualResponse
        {
            Passages = chunks
                .Select(chunk => new ChatManualPassage
                {
                    Location = chunk.Location,
                    Text = AgentDataBlocks.Wrap(ChunkTag, chunk.Text),
                })
                .ToList(),
        };
    }
}
