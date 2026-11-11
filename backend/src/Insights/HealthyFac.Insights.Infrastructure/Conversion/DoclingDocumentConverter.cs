using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Shared.Infrastructure.HttpFailures;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Conversion;

/// <summary>
/// Converts a file with docling-serve: one upload to the async hybrid chunk endpoint (OCR with EasyOCR, table
/// structure, chunks of about 800 tokens with merged peers), a poll of the task, and one read of the result. The
/// request body streams from the given stream, so no copy of the file stays in memory.
/// </summary>
/// <remarks>
/// Each external call runs in one retry unit of <see cref="IProviderCalls"/>: the open of the content with the
/// upload (the upload reads the stream, so a repeat opens it again), each poll, and the read of the result. One
/// transient poll error therefore never starts the upload and the GPU conversion again.
/// The poll runs every <see cref="DoclingOptions.PollSeconds"/>. Before each poll, the method asks the caller whether
/// the user cancelled, and it then stops with an <see cref="OperationCanceledException"/> of a linked source. The
/// request asks for at most <see cref="DoclingOptions.MaxPages"/> plus 1 page, so a longer file shows a page count
/// above the limit without a full conversion. docling-serve returns a result once (it deletes it at the first read).
/// </remarks>
/// <param name="httpClientFactory">Creates the Docling client.</param>
/// <param name="options">Holds the poll interval and the page limit.</param>
/// <param name="timeProvider">Provides the clock of the poll delay.</param>
/// <param name="providers">Runs each external call with the retries.</param>
public sealed class DoclingDocumentConverter(
    IHttpClientFactory httpClientFactory,
    IOptions<DoclingOptions> options,
    TimeProvider timeProvider,
    IProviderCalls providers
) : IDocumentConverter
{
    private const string NotAvailableReason = "Docling gave no reason.";
    private const string NoResultReason = "Docling gave no result.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly DoclingOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IProviderCalls _providers = providers;

    public async Task<ConvertedDocument> ConvertAsync(
        Func<CancellationToken, Task<Stream>> openContent,
        string fileName,
        string extension,
        Func<CancellationToken, Task<bool>> isCancelled,
        CancellationToken cancellationToken
    )
    {
        using var client = _httpClientFactory.CreateClient(DoclingConstants.ClientName);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var taskId = await _providers.ExecuteAsync(
            async token =>
            {
                await using var content = await openContent(token);

                return await SubmitAsync(client, content, fileName, extension, token);
            },
            linked.Token);
        await WaitAsync(client, taskId, isCancelled, linked, cancellationToken);
        var result = await ReadResultAsync(client, taskId, linked.Token);

        return Map(result, extension);
    }

    private static string BuildLocation(DoclingChunk chunk, string extension)
    {
        var label = extension switch
        {
            DoclingConstants.Location.SlideExtension => DoclingConstants.Location.SlideLabel,
            DoclingConstants.Location.SheetExtension => DoclingConstants.Location.SheetLabel,
            _ => DoclingConstants.Location.PageLabel,
        };
        var pages = (chunk.PageNumbers ?? []).Distinct().Order().ToList();
        var pagePart = pages.Count switch
        {
            0 => string.Empty,
            1 => $"{label} {pages[index: 0].ToString(CultureInfo.InvariantCulture)}",
            _ => $"{label}s {pages[index: 0].ToString(CultureInfo.InvariantCulture)}"
                + $"{DoclingConstants.Location.PageRangeSeparator}{pages[^1].ToString(CultureInfo.InvariantCulture)}",
        };
        var headingPart = string.Join(DoclingConstants.Location.HeadingSeparator, chunk.Headings ?? []);

        return string.Join(
            DoclingConstants.Location.PartSeparator,
            new[] { pagePart, headingPart }.Where(part => part.Length > 0));
    }

    private static int CountPages(DoclingChunkResult result)
    {
        var pages = result.Documents.FirstOrDefault()?.Content?.JsonContent?.Pages;
        var count = pages is { ValueKind: JsonValueKind.Object } ? pages.Value.EnumerateObject().Count() : 0;

        return Math.Max(count, val2: 1);
    }

    private ConvertedDocument Map(DoclingChunkResult result, string extension) =>
        new()
        {
            PageCount = CountPages(result),
            Chunks = result.Chunks
                .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Text))
                .Select(chunk => new ConvertedChunk
                {
                    Location = BuildLocation(chunk, extension) is { Length: > 0 } location ? location : null,
                    Text = chunk.Text!,
                })
                .ToList(),
        };

    private async Task<string> SubmitAsync(
        HttpClient client,
        Stream content,
        string fileName,
        string extension,
        CancellationToken cancellationToken
    )
    {
        using var form = new MultipartFormDataContent();
        var filePart = new StreamContent(content);
        filePart.Headers.ContentType = new MediaTypeHeaderValue(mediaType: "application/octet-stream");
        form.Add(filePart, DoclingConstants.FilePartName, $"{fileName}.{extension}");
        AddField(form, DoclingConstants.Fields.IncludeConvertedDocument, bool.TrueString.ToLowerInvariant());
        AddField(form, DoclingConstants.Fields.DoOcr, bool.TrueString.ToLowerInvariant());
        AddField(form, DoclingConstants.Fields.OcrEngine, DoclingConstants.OcrEngine);
        AddField(form, DoclingConstants.Fields.DoTableStructure, bool.TrueString.ToLowerInvariant());
        var maxTokens = DoclingConstants.ChunkMaxTokens.ToString(CultureInfo.InvariantCulture);
        AddField(form, DoclingConstants.Fields.ChunkMaxTokens, maxTokens);
        AddField(form, DoclingConstants.Fields.ChunkMergePeers, bool.TrueString.ToLowerInvariant());
        AddField(form, DoclingConstants.Fields.PageRange, value: "1");
        var lastPage = (_options.MaxPages + 1).ToString(CultureInfo.InvariantCulture);
        AddField(form, DoclingConstants.Fields.PageRange, lastPage);

        using var response = await client.PostAsync(DoclingConstants.SubmitPath, form, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
        var status = await response.Content.ReadFromJsonAsync<DoclingTaskStatus>(JsonOptions, cancellationToken);

        return status?.TaskId ?? throw new ConversionFailedException(NoResultReason);
    }

    private async Task WaitAsync(
        HttpClient client,
        string taskId,
        Func<CancellationToken, Task<bool>> isCancelled,
        CancellationTokenSource linked,
        CancellationToken cancellationToken
    )
    {
        var pollInterval = TimeSpan.FromSeconds(_options.PollSeconds);

        while (true)
        {
            var hasUserCancelled = await isCancelled(cancellationToken);
            if (hasUserCancelled)
            {
                await linked.CancelAsync();
                linked.Token.ThrowIfCancellationRequested();
            }

            var status = await _providers.ExecuteAsync(
                async token =>
                {
                    using var response = await client.GetAsync(
                        string.Format(DoclingConstants.PollPathFormat, taskId),
                        token);
                    await response.EnsureSuccessAsync(token);

                    return await response.Content.ReadFromJsonAsync<DoclingTaskStatus>(JsonOptions, token);
                },
                linked.Token);

            if (status?.TaskStatus == DoclingConstants.Status.Success)
            {
                return;
            }

            if (status is null || status.TaskStatus == DoclingConstants.Status.Failure)
            {
                throw new ConversionFailedException(status?.ErrorMessage ?? NotAvailableReason);
            }

            await Task.Delay(pollInterval, _timeProvider, linked.Token);
        }
    }

    private async Task<DoclingChunkResult> ReadResultAsync(
        HttpClient client,
        string taskId,
        CancellationToken cancellationToken
    )
    {
        var result = await _providers.ExecuteAsync(
            async token =>
            {
                using var response = await client.GetAsync(
                    string.Format(DoclingConstants.ResultPathFormat, taskId),
                    token);
                await response.EnsureSuccessAsync(token);

                return await response.Content.ReadFromJsonAsync<DoclingChunkResult>(JsonOptions, token);
            },
            cancellationToken);

        return result ?? throw new ConversionFailedException(NoResultReason);
    }

    private static void AddField(MultipartFormDataContent form, string name, string value) =>
        form.Add(new StringContent(value), name);
}
