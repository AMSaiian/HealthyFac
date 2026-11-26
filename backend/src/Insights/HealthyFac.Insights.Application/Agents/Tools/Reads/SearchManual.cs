using FluentValidation;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Documents;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Tools.Reads;

/// <summary>The arguments that the model gives <c>search_manual</c>.</summary>
public sealed record SearchManualArguments
{
    public required string Query { get; init; }
}

[AgentTool(role: "planner", ToolName, Description = ToolDescription, IsRead = true)]
[LocalTool]
public sealed record
    SearchManualRequest : IAgentToolRequest<SearchManualRequest, SearchManualArguments, SearchManualResponse>
{
    public const string ToolName = "search_manual";

    public const string ToolDescription =
        "Searches the documents of the machine (manuals, procedures) and returns the passages that are nearest to the "
        + "query, each with its location. A passage that an earlier search of this run returned comes back as "
        + "repeatOf with the number of that call, without its text.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required string Query { get; init; }

    public required Guid RunId { get; init; }

    public required ManualPassageLog PassageLog { get; init; }

    public static SearchManualRequest Create(SearchManualArguments arguments, ToolContext context) =>
        new()
        {
            CompanyId = context.CompanyId,
            MachineId = context.RequireMachineId(),
            Query = arguments.Query,
            RunId = context.RunId,
            PassageLog = context.ManualPassages,
        };
}

public sealed record SearchManualResponse
{
    public required IReadOnlyList<SearchManualPassage> Passages { get; init; }
}

public sealed record SearchManualPassage
{
    public string? Location { get; init; }

    /// <summary>The text of the chunk inside an escaped data block. A repeated passage has none.</summary>
    public string? Text { get; init; }

    /// <summary>The sequence of the earlier call of the run that returned this passage.</summary>
    public int? RepeatOf { get; init; }
}

public class SearchManualRequestValidator : AbstractValidator<SearchManualRequest>
{
    public SearchManualRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.Query).NotEmpty().SingleLineText().WithMessage(AgentMessages.QueryRequired);
    }
}

public sealed class SearchManualPreHandlerCheck(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchManualRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchManualRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Embeds the query and returns the nearest chunks of the documents that are tied to the machine inside the company.
/// The number of chunks is the option <see cref="AgentOptions.ManualSearchChunks"/>. A chunk that only holds titles
/// (<see cref="ManualChunkText.IsTitleOnly"/>) never comes back: the search asks for more chunks until the count is
/// full or the chunks end. A chunk that an earlier call of the run returned comes back as <c>repeatOf</c> with the
/// sequence of that call. Each other chunk text goes into an escaped data block, because a manual is data and never an
/// instruction.
/// </summary>
/// <param name="embedder">Embeds the query.</param>
/// <param name="search">Finds the nearest chunks.</param>
/// <param name="options">Holds the number of chunks.</param>
/// <param name="dbContext">Reads the sequence that the recorder gives the running call.</param>
public sealed class SearchManualRequestHandler(
    ChunkEmbedder embedder,
    IManualSearch search,
    IOptions<AgentOptions> options,
    IInsightsDbContext dbContext
) : IRequestHandler<SearchManualRequest, SearchManualResponse>
{
    public const string ChunkTag = "manual_chunk";

    private readonly ChunkEmbedder _embedder = embedder;
    private readonly IManualSearch _search = search;
    private readonly AgentOptions _options = options.Value;
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<SearchManualResponse> HandleAsync(
        SearchManualRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var vectors = await _embedder.EmbedAsync([request.Query], cancellationToken);
        var chunks = await FindChunksAsync(request, vectors[0], cancellationToken);

        var lastSequence = await _dbContext.AgentToolCalls
            .Where(call => call.RunId == request.RunId)
            .MaxAsync(call => (int?)call.Sequence, cancellationToken);

        var sequence = (lastSequence ?? 0) + 1;
        var passages = new List<SearchManualPassage>();

        foreach (var chunk in chunks)
        {
            if (request.PassageLog.TryGetSequence(chunk.Id, out var earlierSequence))
            {
                passages.Add(new SearchManualPassage { RepeatOf = earlierSequence });

                continue;
            }

            request.PassageLog.Add(chunk.Id, sequence);

            passages.Add(
                new SearchManualPassage
                {
                    Location = chunk.Location,
                    Text = AgentDataBlocks.Wrap(ChunkTag, chunk.Text),
                });
        }

        return new SearchManualResponse { Passages = passages };
    }

    private async Task<IReadOnlyList<ManualChunk>> FindChunksAsync(
        SearchManualRequest request,
        float[] vector,
        CancellationToken cancellationToken
    )
    {
        var wanted = _options.ManualSearchChunks;
        var asked = wanted;

        while (true)
        {
            var found = await _search.SearchAsync(
                request.CompanyId, request.MachineId, vector, asked, cancellationToken);

            var kept = found.Where(chunk => !ManualChunkText.IsTitleOnly(chunk.Text)).Take(wanted).ToList();

            if (kept.Count == wanted || found.Count < asked)
            {
                return kept;
            }

            asked += wanted;
        }
    }
}
