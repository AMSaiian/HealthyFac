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
                name: "embedding service answers 404",
                fixture => fixture.Ingest.Embeddings.Failure =
                    new ClientError(
                        message: "Model not found", statusCode: 404, status: "NOT_FOUND", innerException: null),
                fixture => fixture.Ingest.Embeddings.CallCount,
                FakeDocumentConverter.DefaultDocument.Chunks.Count)
        },
        {
            new ProviderRejectionCase(
                name: "document answers PermissionDenied",
                fixture => fixture.Files.Failure =
                    new RpcException(new Status(StatusCode.PermissionDenied, detail: "No access")),
                fixture => fixture.Files.CallCount,
                expectedCalls: 1)
        },
    };

    public static TheoryData<ProviderRejectionCase> UnexpectedErrors => new()
    {
        {
            new ProviderRejectionCase(
                name: "docling body is not json",
                fixture => fixture.Ingest.Converter.Behavior = _ => throw new JsonException(message: "Bad body"),
                fixture => fixture.Ingest.Converter.CallCount,
                expectedCalls: 1)
        },
        {
            new ProviderRejectionCase(
                name: "embedder gets a wrong vector size",
                fixture => fixture.Ingest.Embeddings.Failure = new InvalidOperationException(message: "Wrong size"),
                fixture => fixture.Ingest.Embeddings.CallCount,
                FakeDocumentConverter.DefaultDocument.Chunks.Count)
        },
    };

    private static ProviderRejectionCase RejectionOfChat(int status) =>
        new(
            $"gemini answers {status}",
            fixture => fixture.Ingest.Chat.Failure = new ClientError(
                message: "Model not found", status, status: "NOT_FOUND", innerException: null),
            fixture => fixture.Ingest.Chat.CallCount,
            expectedCalls: 1);

    public static TheoryData<IngestFailureCase> FailureCases => new()
    {
        {
            new IngestFailureCase(
                name: "file link is missing",
                IngestErrorCode.FileNotFound,
                fixture => fixture.Files.Clear())
        },
        {
            new IngestFailureCase(
                name: "document has no text",
                IngestErrorCode.NoText,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    Task.FromResult(new ConvertedDocument { PageCount = 1, Chunks = [] }))
        },
        {
            new IngestFailureCase(
                name: "document has too many pages",
                IngestErrorCode.TooManyPages,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    Task.FromResult(FakeDocumentConverter.DefaultDocument with { PageCount = TooManyPages }))
        },
        {
            new IngestFailureCase(
                name: "conversion fails",
                IngestErrorCode.ConversionFailed,
                fixture => fixture.Ingest.Converter.Behavior = _ =>
                    throw new ConversionFailedException(message: "Unsupported layout"))
        },
        {
            new IngestFailureCase(
                name: "model makes invalid tool calls in a row",
                IngestErrorCode.InvalidExtraction,
                fixture => fixture.Ingest.Chat.ScriptInvalidToolCalls(count: 3))
        },
        {
            new IngestFailureCase(
                name: "embedding service stays unavailable",
                IngestErrorCode.ProviderUnavailable,
                fixture => fixture.Ingest.Embeddings.Failure =
                    new HttpRequestException(
                        message: "Embedding is down", inner: null, HttpStatusCode.ServiceUnavailable))
        },
        {
            new IngestFailureCase(
                name: "screener stays unavailable",
                IngestErrorCode.ProviderUnavailable,
                fixture => fixture.Ingest.ScreenerChat.Failure =
                    new HttpRequestException(
                        message: "Screener is down", inner: null, HttpStatusCode.ServiceUnavailable))
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
