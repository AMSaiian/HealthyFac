using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Runs the stages of one ingest: the link of the file, the conversion, the unit catalog and the machine signals, the
/// embeddings, the extraction, and the mapping. Each external call goes through <see cref="IProviderCalls"/>. Between
/// the stages, the pipeline asks whether the attempt is still active.
/// </summary>
/// <param name="fileCatalog">Reads the file and its signed link from Document.</param>
/// <param name="blobSource">Opens the content from the link.</param>
/// <param name="converter">Converts the file to chunks.</param>
/// <param name="unitCatalog">Reads the unit catalog.</param>
/// <param name="signalCatalog">Reads the metric keys and status codes of the machine.</param>
/// <param name="embedder">Embeds the chunk texts.</param>
/// <param name="extractor">Asks the model for machines, metrics, and statuses.</param>
/// <param name="mapper">Maps the answer to draft messages.</param>
/// <param name="providers">Runs the external calls with retries.</param>
/// <param name="options">Holds the page limit.</param>
/// <param name="metrics">Records the duration of each stage.</param>
public sealed class IngestPipeline(
    IFileCatalog fileCatalog,
    IBlobStreamSource blobSource,
    IDocumentConverter converter,
    IMetricUnitCatalog unitCatalog,
    IMachineSignalCatalog signalCatalog,
    ChunkEmbedder embedder,
    IDraftExtractor extractor,
    DraftProposalMapper mapper,
    IProviderCalls providers,
    IOptions<IngestProcessingOptions> options,
    IngestMetrics metrics
)
{
    private readonly IFileCatalog _fileCatalog = fileCatalog;
    private readonly IBlobStreamSource _blobSource = blobSource;
    private readonly IDocumentConverter _converter = converter;
    private readonly IMetricUnitCatalog _unitCatalog = unitCatalog;
    private readonly IMachineSignalCatalog _signalCatalog = signalCatalog;
    private readonly ChunkEmbedder _embedder = embedder;
    private readonly IDraftExtractor _extractor = extractor;
    private readonly DraftProposalMapper _mapper = mapper;
    private readonly IProviderCalls _providers = providers;
    private readonly IngestProcessingOptions _options = options.Value;
    private readonly IngestMetrics _metrics = metrics;

    /// <summary>
    /// Runs all stages.
    /// </summary>
    /// <param name="attempt">The claimed attempt.</param>
    /// <param name="isNoLongerActive">Reads the state of the attempt: <see langword="true"/> when it left
    /// <c>inProcess</c>.</param>
    /// <param name="cancellationToken">The token that stops the run (a stop of the service or the run limit).</param>
    /// <returns>The converted document, the embeddings, and the mapped drafts.</returns>
    /// <exception cref="IngestFailureException">Thrown with the error code of the stage that failed.</exception>
    /// <exception cref="ProviderUnavailableException">Thrown when an external service stayed unavailable.</exception>
    /// <exception cref="AttemptNoLongerActiveException">Thrown when the attempt left <c>inProcess</c>.</exception>
    public async Task<IngestPipelineResult> RunAsync(
        IngestAttemptSnapshot attempt,
        Func<CancellationToken, Task<bool>> isNoLongerActive,
        CancellationToken cancellationToken
    )
    {
        var link = await _providers.ExecuteAsync(
                token => _fileCatalog.GetFileLinkForWorkerAsync(attempt.FileId, token),
                cancellationToken)
            ?? throw new IngestFailureException(IngestErrorCode.FileNotFound);
        await EnsureActiveAsync(isNoLongerActive, cancellationToken);

        var converted = await ConvertAsync(attempt, link, isNoLongerActive, cancellationToken);
        if (converted.PageCount > _options.MaxPages)
        {
            throw new IngestFailureException(IngestErrorCode.TooManyPages);
        }

        if (converted.Chunks.Count == 0)
        {
            throw new IngestFailureException(IngestErrorCode.NoText);
        }

        await EnsureActiveAsync(isNoLongerActive, cancellationToken);
        var units = await _providers.ExecuteAsync(_unitCatalog.GetAllAsync, cancellationToken);
        var signals = await ReadSignalsAsync(link.File.MachineId, cancellationToken);
        await EnsureActiveAsync(isNoLongerActive, cancellationToken);

        var texts = converted.Chunks.Select(chunk => chunk.Text).ToList();
        var embeddings = await EmbedAsync(texts, cancellationToken);
        await EnsureActiveAsync(isNoLongerActive, cancellationToken);

        var isMachineFile = link.File.MachineId is not null;
        var mapped = await ExtractAndMapAsync(
            attempt,
            link,
            converted,
            units,
            signals,
            isMachineFile,
            cancellationToken);
        await EnsureActiveAsync(isNoLongerActive, cancellationToken);

        return new IngestPipelineResult
        {
            MachineId = link.File.MachineId,
            MachineChangedAt = link.File.MachineChangedAt,
            PageCount = converted.PageCount,
            Chunks = converted.Chunks,
            Embeddings = embeddings,
            Mapped = mapped,
        };
    }

    private static async Task EnsureActiveAsync(
        Func<CancellationToken, Task<bool>> isNoLongerActive,
        CancellationToken cancellationToken
    )
    {
        var isInactive = await isNoLongerActive(cancellationToken);
        if (isInactive)
        {
            throw new AttemptNoLongerActiveException();
        }
    }

    private async Task<ConvertedDocument> ConvertAsync(
        IngestAttemptSnapshot attempt,
        IngestFileLink link,
        Func<CancellationToken, Task<bool>> isNoLongerActive,
        CancellationToken cancellationToken
    )
    {
        using var span = IngestActivitySource.Instance.StartActivity(ObservabilityConstants.Ingest.Spans.Convert);
        using var timer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Convert);

        try
        {
            return await _converter.ConvertAsync(
                token => _blobSource.OpenAsync(link.Url, token),
                attempt.FileName,
                attempt.Extension,
                isNoLongerActive,
                cancellationToken);
        }
        catch (ConversionFailedException exception)
        {
            throw new IngestFailureException(IngestErrorCode.ConversionFailed, exception);
        }
    }

    private async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken
    )
    {
        using var timer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Embed);

        return await _embedder.EmbedAsync(texts, cancellationToken);
    }

    private async Task<MachineSignals> ReadSignalsAsync(Guid? machineId, CancellationToken cancellationToken)
    {
        if (machineId is null)
        {
            return new MachineSignals { MetricKeys = [], StatusCodes = [] };
        }

        return await _providers.ExecuteAsync(
            token => _signalCatalog.GetAsync(machineId.Value, token),
            cancellationToken);
    }

    private async Task<MappingResult> ExtractAndMapAsync(
        IngestAttemptSnapshot attempt,
        IngestFileLink link,
        ConvertedDocument converted,
        IReadOnlyList<MetricUnit> units,
        MachineSignals signals,
        bool isMachineFile,
        CancellationToken cancellationToken
    )
    {
        using var timer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Extract);

        try
        {
            var input = new ExtractionInput
            {
                Chunks = converted.Chunks,
                Units = units,
                ExistingMetricKeys = signals.MetricKeys,
                ExistingStatusCodes = signals.StatusCodes,
                IsMachineFile = isMachineFile,
            };
            var extraction = await _providers.ExecuteAsync(
                token => _extractor.ExtractAsync(input, token),
                cancellationToken);

            return _mapper.Map(
                extraction,
                new MappingContext
                {
                    AttemptId = attempt.AttemptId,
                    FileId = attempt.FileId,
                    CompanyId = attempt.CompanyId,
                    IsMachineFile = isMachineFile,
                    FileMachineId = link.File.MachineId,
                    QueuedAt = attempt.QueuedAt,
                    Units = units,
                });
        }
        catch (InvalidExtractionException exception)
        {
            throw new IngestFailureException(IngestErrorCode.InvalidExtraction, exception);
        }
    }
}
