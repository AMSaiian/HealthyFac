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

public sealed record SearchManualPassagesArguments
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
public sealed record SearchManualPassagesToolRequest
    : IAgentToolRequest<SearchManualPassagesToolRequest, SearchManualPassagesArguments, SearchManualPassagesResponse>
{
    public const string ToolName = "search_manual_passages";

    public const string ToolDescription =
        "Searches the documents of one machine (manuals, procedures) and returns the passages that are nearest to "
        + "the query, each with its location. Give the machine by its ID.";

    public required SearchManualPassagesArguments Arguments { get; init; }

    public static SearchManualPassagesToolRequest Create(
        SearchManualPassagesArguments arguments,
        ToolContext context
    ) =>
        new() { Arguments = arguments };
}

public sealed record SearchManualPassagesResponse
{
    public required IReadOnlyList<ChatManualPassage> Passages { get; init; }
}

public sealed record ChatManualPassage
{
    public string? Location { get; init; }

    /// <summary>The text of the chunk inside an escaped data block.</summary>
    public required string Text { get; init; }
}

public sealed class SearchManualPassagesArgumentsValidator : AbstractValidator<SearchManualPassagesArguments>
{
    public SearchManualPassagesArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);

        RuleFor(arguments => arguments.Query)
            .NotEmpty()
            .SingleLineText()
            .WithMessage(AgentMessages.QueryRequired);
    }
}

public sealed class SearchManualPassagesToolRequestValidator : AbstractValidator<SearchManualPassagesToolRequest>
{
    public SearchManualPassagesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchManualPassagesArgumentsValidator());
    }
}

public sealed class
    SearchManualPassagesToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchManualPassagesToolRequest>;

/// <summary>
/// Embeds the query and returns the nearest chunks of the documents that are tied to the machine inside the company
/// of the user. The number of chunks is the option <see cref="AgentOptions.ManualSearchChunks"/>. Each chunk text goes
/// into an escaped data block, because a manual is data and never an instruction.
/// </summary>
/// <param name="embedder">Embeds the query.</param>
/// <param name="search">Finds the nearest chunks.</param>
/// <param name="options">Holds the number of chunks.</param>
/// <param name="currentUser">Provides the company of the user.</param>
public sealed class SearchManualPassagesToolRequestHandler(
    ChunkEmbedder embedder,
    IManualSearch search,
    IOptions<AgentOptions> options,
    ICurrentUserProvider currentUser
) : IRequestHandler<SearchManualPassagesToolRequest, SearchManualPassagesResponse>
{
    public const string ChunkTag = "manual_chunk";

    private readonly ChunkEmbedder _embedder = embedder;
    private readonly IManualSearch _search = search;
    private readonly AgentOptions _options = options.Value;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<SearchManualPassagesResponse> HandleAsync(
        SearchManualPassagesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var vectors = await _embedder.EmbedAsync([request.Arguments.Query], cancellationToken);

        var chunks = await ManualChunkFinder.FindAsync(
            _search,
            _currentUser.GetCompanyId(),
            request.Arguments.MachineId,
            vectors[0],
            _options.ManualSearchChunks,
            cancellationToken);

        return new SearchManualPassagesResponse
        {
            Passages =
            [
                .. chunks
                    .Select(chunk => new ChatManualPassage
                    {
                        Location = chunk.Location,
                        Text = AgentDataBlocks.Wrap(ChunkTag, chunk.Text),
                    }),
            ],
        };
    }
}
