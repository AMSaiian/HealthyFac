using System.Runtime.InteropServices;
using HealthyFac.Document.Application.Files.Uploading;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Documents;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.Insights.Messaging.Ingest;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.Ingest;

/// <summary>
/// Seeds the documents of the ingest sets and their ingest results. An upload goes through the use case of Document,
/// so the blob reaches the storage. The result of the ingest (the completed attempt, the document with its chunks and
/// vectors, the agent runs, and the tool calls) goes through the domain of Insights, because the agents need an AI
/// model. The same save adds the messages that a completed attempt sends. The relay then stores the machine drafts in
/// Core, the definition drafts in Telemetry, and the ingest state in Document.
/// </summary>
public sealed class IngestPart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly Dictionary<string, Func<SeedContext, CancellationToken, Task>> _actions = [];

    private SeedFolder _folder = null!;
    private IngestEmbeddingsFile _embeddings = null!;

    public IngestPart(ServiceHosts.ServiceHosts hosts)
    {
        _hosts = hosts;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        _folder = folder;
        _actions.Clear();
        _embeddings = folder.Read<IngestEmbeddingsFile>(IngestConstants.EmbeddingsFileName);
        var events = new List<SeedEvent>();

        foreach (var setNumber in IngestConstants.SetNumbers)
        {
            var fileName = string.Format(IngestConstants.SetFilePattern, setNumber);

            foreach (var entry in folder.Read<IngestSetFile>(fileName).Entries)
            {
                events.Add(UploadEvent(fileName, entry));
                events.Add(IngestEvent(fileName, entry));
            }
        }

        return events;
    }

    public Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken) =>
        _actions[seedEvent.Item](context, cancellationToken);

    private static string UserKey(string person) => $"{person}{PeopleConstants.UserSuffix}";

    private static string ContentTypeOf(string item, string path)
    {
        var extension = Path.GetExtension(path);

        return IngestConstants.ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : throw new SeedFileException(IngestErrors.ContentTypeUnknown(item, extension));
    }

    private static float[] Decode(string base64) =>
        MemoryMarshal.Cast<byte, float>(Convert.FromBase64String(base64)).ToArray();

    private SeedEvent UploadEvent(string fileName, IngestEntry entry)
    {
        var item = SeedItem.Of(fileName, $"{entry.File.Key}{IngestConstants.UploadSuffix}");
        _actions[item] = (context, cancellationToken) => UploadAsync(item, entry, context, cancellationToken);

        return new SeedEvent
        {
            At = entry.File.UploadedAt,
            Actor = entry.File.UploadedBy,
            Item = item,
            Creates = [FileKey(entry)],
            Uses = [PeopleConstants.CompanyKey, UserKey(entry.File.UploadedBy)],
            Requests =
            [
                new SeedRequest
                {
                    Service = ServiceName.Document,
                    Request = UploadRequest(item, entry, PeopleConstants.CheckId),
                },
            ],
        };
    }

    private SeedEvent IngestEvent(string fileName, IngestEntry entry)
    {
        var item = SeedItem.Of(fileName, $"{entry.File.Key}{IngestConstants.IngestSuffix}");
        _actions[item] = (context, cancellationToken) => IngestAsync(entry, context, cancellationToken);

        return new SeedEvent
        {
            At = entry.QueuedAt,
            Actor = entry.File.UploadedBy,
            Item = item,
            Creates = [AttemptKey(entry)],
            Uses = [PeopleConstants.CompanyKey, UserKey(entry.File.UploadedBy), FileKey(entry)],
        };
    }

    private static string FileKey(IngestEntry entry) => $"{entry.File.Key}{IngestConstants.FileSuffix}";

    private static string AttemptKey(IngestEntry entry) => $"{entry.File.Key}{IngestConstants.AttemptSuffix}";

    private UploadFileRequest UploadRequest(string item, IngestEntry entry, Guid companyId)
    {
        var bytes = _folder.ReadBytes(entry.File.Path);

        return new UploadFileRequest
        {
            CompanyId = companyId,
            File = new FileUpload
            {
                Content = new MemoryStream(bytes),
                ClientFileName = Path.GetFileName(entry.File.Path),
                DeclaredContentType = ContentTypeOf(item, entry.File.Path),
                Length = bytes.Length,
            },
        };
    }

    private async Task UploadAsync(
        string item,
        IngestEntry entry,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var request = UploadRequest(item, entry, context.GetId(PeopleConstants.CompanyKey));

        var response = await _hosts.Document.SendAsync<UploadFileRequest, UploadFileResponse>(
            context.GetActor(entry.File.UploadedBy),
            request,
            cancellationToken);

        context.SetId(FileKey(entry), response.Id);
    }

    private async Task IngestAsync(IngestEntry entry, SeedContext context, CancellationToken cancellationToken)
    {
        var actor = context.GetActor(entry.File.UploadedBy);
        var companyId = context.GetId(PeopleConstants.CompanyKey);
        var fileId = context.GetId(FileKey(entry));
        var uploadedAt = SeedCalendar.ToUtc(entry.File.UploadedAt, context.Anchor, context.Zone).UtcDateTime;
        using var scope = _hosts.Insights.CreateScope(actor);
        var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();
        var queuedAt = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var finishedAt = queuedAt + entry.Attempt.FinishedAfter;

        var attempt = AddAttempt(dbContext, entry, companyId, fileId, uploadedAt, queuedAt, finishedAt);
        AddDocument(dbContext, entry, companyId, fileId, uploadedAt);

        foreach (var run in entry.Runs)
        {
            AddRun(dbContext, run, attempt.Id, queuedAt);
        }

        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        outbox.Add(entry.DraftsProposed.ToMessage(attempt.Id, fileId, companyId, queuedAt));

        outbox.Add(
            new IngestStateChanged
            {
                FileId = fileId,
                CompanyId = companyId,
                State = IngestMessageStates.Completed,
                FailedCount = 0,
                ChangedAt = finishedAt,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        context.SetId(AttemptKey(entry), attempt.Id);
    }

    private IngestAttempt AddAttempt(
        InsightsDbContext dbContext,
        IngestEntry entry,
        Guid companyId,
        Guid fileId,
        DateTime uploadedAt,
        DateTime queuedAt,
        DateTime finishedAt
    )
    {
        var bytes = _folder.ReadBytes(entry.File.Path);

        var attempt = new IngestAttempt(
            companyId,
            fileId,
            machineId: null,
            uploadedAt,
            Path.GetFileNameWithoutExtension(entry.File.Path),
            Path.GetExtension(entry.File.Path)[1..].ToLowerInvariant(),
            bytes.Length,
            queuedAt,
            traceParent: null);

        attempt.Claim(queuedAt + entry.Attempt.StartedAfter, finishedAt);

        attempt.Complete(
            finishedAt,
            entry.Attempt.PageCount,
            entry.Chunks.Count,
            entry.Attempt.ProposedDraftCount,
            warning: null);

        dbContext.IngestAttempts.Add(attempt).Property(nameof(IngestAttempt.Id)).CurrentValue = entry.Attempt.Id;

        return attempt;
    }

    private void AddDocument(
        InsightsDbContext dbContext,
        IngestEntry entry,
        Guid companyId,
        Guid fileId,
        DateTime uploadedAt
    )
    {
        var document = new IngestedDocument(
            fileId,
            companyId,
            machineId: null,
            uploadedAt,
            entry.Attempt.Id,
            Path.GetExtension(entry.File.Path)[1..].ToLowerInvariant(),
            entry.Attempt.PageCount);

        var chunks = entry.Chunks.OrderBy(chunk => chunk.Ordinal).ToList();

        foreach (var chunk in chunks)
        {
            document.AddChunk(chunk.Location, chunk.Text, Decode(FindVector(chunk.Id)));
        }

        dbContext.IngestedDocuments.Add(document);

        foreach (var (chunk, seed) in document.Chunks.Zip(chunks))
        {
            dbContext.Entry(chunk).Property(nameof(DocumentChunk.Id)).CurrentValue = seed.Id;
        }
    }

    private string FindVector(Guid chunkId) =>
        _embeddings.Vectors.TryGetValue(chunkId, out var vector)
            ? vector
            : throw new InvalidOperationException(IngestErrors.VectorMissing(chunkId));

    private static void AddRun(InsightsDbContext dbContext, IngestRunSeed seed, Guid attemptId, DateTime queuedAt)
    {
        var startedAt = queuedAt + seed.StartedAfter;
        var run = new AgentRun(AgentRole.FromValue(seed.Role), caseId: null, attemptId, queuedAt + seed.DueAfter);
        run.Claim(startedAt, startedAt);
        run.UseModel(seed.Model, seed.PromptVersion, seed.PromptHash);

        run.Complete(
            queuedAt + seed.FinishedAfter,
            verdict: null,
            verdictReason: null,
            seed.InputTokens,
            seed.OutputTokens);

        dbContext.AgentRuns.Add(run).Property(nameof(AgentRun.Id)).CurrentValue = seed.Id;

        foreach (var callSeed in seed.ToolCalls.OrderBy(call => call.Sequence))
        {
            var call = new AgentToolCall(
                seed.Id,
                callSeed.Sequence,
                callSeed.CallId,
                callSeed.ToolName,
                callSeed.Arguments.GetRawText(),
                ToolCallOutcome.FromValue(callSeed.Outcome),
                callSeed.Result.GetRawText(),
                callSeed.DurationMilliseconds);

            dbContext.AgentToolCalls.Add(call).Property(nameof(AgentToolCall.Id)).CurrentValue = callSeed.Id;
        }
    }
}
