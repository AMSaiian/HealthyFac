using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Embeddings;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Extraction.Mapping;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Application.Extraction.Runs;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Application.Ingest.Processing.Monitoring;
using HealthyFac.Insights.Application.Ingest.Processing.ProviderCalls;
using HealthyFac.Insights.Application.Ingest.Screening;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Ingest.Processing.Pipeline;

/// <summary>
/// Runs the stages of one ingest: the link of the file, the conversion, the screening, the unit catalog and the machine
/// signals, the embeddings, the extraction, and the mapping. Each external call goes through
/// <see cref="IProviderCalls"/>. Between the stages, the pipeline asks whether the attempt is still active.
/// </summary>
/// <param name="fileCatalog">Reads the file and its signed link from Document.</param>
/// <param name="blobSource">Opens the content from the link.</param>
/// <param name="converter">Converts the file to chunks.</param>
/// <param name="unitCatalog">Reads the unit catalog.</param>
/// <param name="signalCatalog">Reads the metric keys and status codes of the machine.</param>
/// <param name="screener">Screens the chunks for text that tries to control a reader.</param>
/// <param name="embedder">Embeds the chunk texts.</param>
/// <param name="extractor">Asks the model for machines, metrics, and statuses.</param>
/// <param name="mapper">Maps the answer to draft messages.</param>
/// <param name="providers">Runs the external calls with retries.</param>
/// <param name="options">Holds the page limit.</param>
/// <param name="metrics">Records the duration of each stage.</param>
/// <param name="timeProvider">Gives the times of the extractor run.</param>
public sealed class IngestPipeline(
    IFileCatalog fileCatalog,
    IBlobStreamSource blobSource,
    IDocumentConverter converter,
    IMetricUnitCatalog unitCatalog,
    IMachineSignalCatalog signalCatalog,
    IChunkScreener screener,
    ChunkEmbedder embedder,
    IDraftExtractor extractor,
    DraftProposalMapper mapper,
    IProviderCalls providers,
    IOptions<IngestProcessingOptions> options,
    IngestMetrics metrics,
    TimeProvider timeProvider
)
{
    private readonly IFileCatalog _fileCatalog = fileCatalog;
    private readonly IBlobStreamSource _blobSource = blobSource;
    private readonly IDocumentConverter _converter = converter;
    private readonly IMetricUnitCatalog _unitCatalog = unitCatalog;
    private readonly IMachineSignalCatalog _signalCatalog = signalCatalog;
    private readonly IChunkScreener _screener = screener;
    private readonly ChunkEmbedder _embedder = embedder;
    private readonly IDraftExtractor _extractor = extractor;
    private readonly DraftProposalMapper _mapper = mapper;
    private readonly IProviderCalls _providers = providers;
    private readonly IngestProcessingOptions _options = options.Value;
    private readonly IngestMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Runs all stages.
    /// </summary>
    /// <param name="attempt">The claimed attempt.</param>
    /// <param name="isNoLongerActive">Reads the state of the attempt: <see langword="true"/> when it left
    /// <c>inProcess</c>.</param>
    /// <param name="screening">Receives the log of the screening, for the <c>screener</c> run.</param>
    /// <param name="extraction">Receives the log of the extraction, for the <c>extractor</c> run.</param>
    /// <param name="cancellationToken">The token that stops the run (a stop of the service or the run limit).</param>
    /// <returns>The converted document, the embeddings, and the mapped drafts.</returns>
    /// <exception cref="IngestFailureException">
    /// Thrown with the error code of the stage that failed. The screening gives <c>unsafeContent</c> with the detail.
    /// </exception>
    /// <exception cref="ProviderUnavailableException">Thrown when an external service stayed unavailable.</exception>
    /// <exception cref="AttemptNoLongerActiveException">Thrown when the attempt left <c>inProcess</c>.</exception>
    public async Task<IngestPipelineResult> RunAsync(
        IngestAttemptSnapshot attempt,
        Func<CancellationToken, Task<bool>> isNoLongerActive,
        ScreeningRunHolder screening,
        ExtractionRunHolder extraction,
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
        await ScreenAsync(attempt, converted, screening, cancellationToken);
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
            extraction,
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

    private async Task ScreenAsync(
        IngestAttemptSnapshot attempt,
        ConvertedDocument converted,
        ScreeningRunHolder holder,
        CancellationToken cancellationToken
    )
    {
        using var timer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Screen);

        ScreeningResult result;

        try
        {
            result = await _screener.ScreenAsync(attempt, converted.Chunks, cancellationToken);
        }
        catch (ProviderUnavailableException exception)
        {
            holder.Log = exception.ScreeningRun;

            throw;
        }

        holder.Log = result.Run;

        if (!result.IsSafe)
        {
            throw new IngestFailureException(IngestErrorCode.UnsafeContent)
            {
                Detail = new IngestErrorDetail { Location = result.UnsafeLocation!, Reason = result.UnsafeReason! },
            };
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
            return new MachineSignals { Metrics = [], Statuses = [] };
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
        ExtractionRunHolder holder,
        CancellationToken cancellationToken
    )
    {
        using var timer = _metrics.MeasureStage(ObservabilityConstants.Ingest.Stages.Extract);
        var log = new ExtractionRunLog(attempt.AttemptId, _timeProvider.GetUtcNow().UtcDateTime);
        holder.Log = log;

        try
        {
            var buffer = new ExtractionBuffer(isMachineFile, link.File.MachineId, units, signals);

            var input = new ExtractionInput
            {
                CompanyId = attempt.CompanyId,
                Chunks = converted.Chunks,
                Units = units,
                Signals = signals,
                IsMachineFile = isMachineFile,
            };

            await _extractor.ExtractAsync(
                input, new ExtractionSession { Buffer = buffer, Log = log }, cancellationToken);

            var mapped = _mapper.Map(
                buffer,
                new MappingContext
                {
                    AttemptId = attempt.AttemptId,
                    FileId = attempt.FileId,
                    CompanyId = attempt.CompanyId,
                    IsMachineFile = isMachineFile,
                    FileMachineId = link.File.MachineId,
                    QueuedAt = attempt.QueuedAt,
                });

            log.Complete(_timeProvider.GetUtcNow().UtcDateTime);

            return mapped;
        }
        catch (InvalidExtractionException exception)
        {
            log.Fail(_timeProvider.GetUtcNow().UtcDateTime, AgentRunErrorCode.UnexpectedError);

            throw new IngestFailureException(IngestErrorCode.InvalidExtraction, exception);
        }
        catch (ProviderUnavailableException)
        {
            log.Fail(_timeProvider.GetUtcNow().UtcDateTime, AgentRunErrorCode.ProviderUnavailable);

            throw;
        }
    }
}
