using System.Net;
using System.Text.Json;
using Google.GenAI;
using Grpc.Core;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Domain.Ingest;
using HealthyFac.Insights.IntegrationTests.Database.Data;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

public static class IngestWorkerTestData
{
    public const int TooManyPages = 501;
    public const int ProviderTryCount = 4;

    public static readonly Guid UserId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000401");

    public static TheoryData<ProviderRejectionCase> RejectionCases => new()
    {
        { RejectionOfChat(status: 400) },
        { RejectionOfChat(status: 403) },
        { RejectionOfChat(status: 404) },
        {
            new ProviderRejectionCase(
                "embedding service answers 404",
                fixture => fixture.Ingest.Embeddings.Failure =
                    new ClientError("Model not found", 404, "NOT_FOUND", null),
                fixture => fixture.Ingest.Embeddings.CallCount,
                expectedCalls: FakeDocumentConverter.DefaultDocument.Chunks.Count)
        },
        {
            new ProviderRejectionCase(
                "document answers PermissionDenied",
                fixture => fixture.Files.Failure =
                    new RpcException(new Status(StatusCode.PermissionDenied, "No access")),
                fixture => fixture.Files.CallCount,
                expectedCalls: 1)
        },
    };

    public static TheoryData<ProviderRejectionCase> UnexpectedErrors => new()
    {
        {
            new ProviderRejectionCase(
                "docling body is not json",
                fixture => fixture.Ingest.Converter.Behavior = _ => throw new JsonException("Bad body"),
                fixture => fixture.Ingest.Converter.CallCount,
                expectedCalls: 1)
        },
        {
            new ProviderRejectionCase(
                "embedder gets a wrong vector size",
                fixture => fixture.Ingest.Embeddings.Failure = new InvalidOperationException("Wrong size"),
                fixture => fixture.Ingest.Embeddings.CallCount,
                expectedCalls: FakeDocumentConverter.DefaultDocument.Chunks.Count)
        },
    };

    private static ProviderRejectionCase RejectionOfChat(int status) =>
        new(
            $"gemini answers {status}",
            fixture => fixture.Ingest.Chat.Failure = new ClientError("Model not found", status, "NOT_FOUND", null),
            fixture => fixture.Ingest.Chat.CallCount,
            expectedCalls: 1);

    public static TheoryData<IngestFailureCase> FailureCases => new()
    {
        {
            new IngestFailureCase(
                "file link is missing",
                IngestErrorCode.FileNotFound,
                fixture => fixture.Files.Clear())
        },
        {
            new IngestFailureCase(
                "document has no text",
                IngestErrorCode.NoText,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    Task.FromResult(new ConvertedDocument { PageCount = 1, Chunks = [] }))
        },
        {
            new IngestFailureCase(
                "document has too many pages",
                IngestErrorCode.TooManyPages,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    Task.FromResult(FakeDocumentConverter.DefaultDocument with { PageCount = TooManyPages }))
        },
        {
            new IngestFailureCase(
                "conversion fails",
                IngestErrorCode.ConversionFailed,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    throw new ConversionFailedException("Unsupported layout"))
        },
        {
            new IngestFailureCase(
                "model answers twice with invalid json",
                IngestErrorCode.InvalidExtraction,
                fixture => fixture.Ingest.Chat.Script(
                    ScriptedChatClient.InvalidAnswer, ScriptedChatClient.InvalidAnswer))
        },
        {
            new IngestFailureCase(
                "embedding service stays unavailable",
                IngestErrorCode.ProviderUnavailable,
                fixture => fixture.Ingest.Embeddings.Failure =
                    new HttpRequestException("Embedding is down", inner: null, HttpStatusCode.ServiceUnavailable))
        },
    };
}

/// <summary>
/// One failure of the ingest: how a test breaks the run and which error code the attempt gets.
/// </summary>
public sealed class IngestFailureCase(string name, IngestErrorCode expectedCode, Action<InsightsFixture> arrange)
{
    public IngestErrorCode ExpectedCode { get; } = expectedCode;

    public void Arrange(InsightsFixture fixture) => arrange(fixture);

    public override string ToString() => name;
}

/// <summary>
/// One rejected or unexpected call: how a test breaks it, how it counts the calls, and how many calls it expects.
/// </summary>
public sealed class ProviderRejectionCase(
    string name,
    Action<InsightsFixture> arrange,
    Func<InsightsFixture, int> callCount,
    int expectedCalls
)
{
    public int ExpectedCalls { get; } = expectedCalls;

    public void Arrange(InsightsFixture fixture) => arrange(fixture);

    public int CallCount(InsightsFixture fixture) => callCount(fixture);

    public override string ToString() => name;
}
