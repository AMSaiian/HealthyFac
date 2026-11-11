using System.Text.Json;

namespace HealthyFac.Insights.Infrastructure.Conversion;

/// <summary>
/// The status of a docling-serve task (<c>task_status</c>: <c>pending</c>, <c>started</c>, <c>success</c>,
/// <c>failure</c>).
/// </summary>
internal sealed record DoclingTaskStatus
{
    public required string TaskId { get; init; }

    public required string TaskStatus { get; init; }

    public string? ErrorMessage { get; init; }
}

/// <summary>
/// The result of a hybrid chunk task: the chunks and the converted documents.
/// </summary>
internal sealed record DoclingChunkResult
{
    public IReadOnlyList<DoclingChunk> Chunks { get; init; } = [];

    public IReadOnlyList<DoclingConvertedDocument> Documents { get; init; } = [];
}

internal sealed record DoclingChunk
{
    public string? Text { get; init; }

    public IReadOnlyList<string>? Headings { get; init; }

    public IReadOnlyList<int>? PageNumbers { get; init; }
}

internal sealed record DoclingConvertedDocument
{
    public DoclingDocumentContent? Content { get; init; }
}

internal sealed record DoclingDocumentContent
{
    public DoclingJsonContent? JsonContent { get; init; }
}

/// <summary>
/// The part of the Docling document JSON that Insights reads: the <c>pages</c> object, which has one property for each
/// page, sheet, or slide.
/// </summary>
internal sealed record DoclingJsonContent
{
    public JsonElement? Pages { get; init; }
}
