using System.Diagnostics;
using System.Security.Cryptography;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Files.Inspection;
using HealthyFac.Document.UnitTests.Files.Data;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Testing.Tracing;

namespace HealthyFac.Document.UnitTests.Files;

public sealed class FileInspectorTests
{
    private readonly FileSignaturesFileInspector _inspector = new();

    [Theory]
    [MemberData(nameof(FileInspectorTestData.AcceptedAttachments), MemberType = typeof(FileInspectorTestData))]
    public async Task InspectAsync_WithAllowedAttachment_ReturnsItsType(
        string name,
        byte[] content,
        string extension,
        string declaredType
    )
    {
        using var stream = new MemoryStream(content);

        var inspection = await _inspector.InspectAsync(
            stream, extension, declaredType, FilePurpose.Attachment, TestContext.Current.CancellationToken);

        inspection.Type.Should().NotBeNull(name);
        inspection.Type!.Extension.Should().Be(AllowedFileTypes.FindByExtension(extension)!.Extension);
    }

    [Theory]
    [MemberData(nameof(FileInspectorTestData.RefusedAttachments), MemberType = typeof(FileInspectorTestData))]
    public async Task InspectAsync_WithRefusedFile_ReturnsNoType(
        string name,
        byte[] content,
        string extension,
        string declaredType
    )
    {
        using var stream = new MemoryStream(content);

        var inspection = await _inspector.InspectAsync(
            stream, extension, declaredType, FilePurpose.Attachment, TestContext.Current.CancellationToken);

        inspection.Type.Should().BeNull(name);
    }

    [Fact]
    public async Task InspectAsync_WithPngForEmbeddedPurpose_ReturnsItsType()
    {
        using var stream = new MemoryStream(SampleFiles.Png);

        var inspection = await _inspector.InspectAsync(
            stream, clientExtension: "png", declaredContentType: "image/png", FilePurpose.Embedded,
            TestContext.Current.CancellationToken);

        inspection.Type.Should().NotBeNull();
    }

    [Fact]
    public async Task InspectAsync_WithPdfForEmbeddedPurpose_ReturnsNoType()
    {
        using var stream = new MemoryStream(SampleFiles.Pdf);

        var inspection = await _inspector.InspectAsync(
            stream, clientExtension: "pdf", declaredContentType: "application/pdf", FilePurpose.Embedded,
            TestContext.Current.CancellationToken);

        inspection.Type.Should().BeNull();
    }

    [Fact]
    public async Task InspectAsync_WithContent_ReturnsHashAndSizeAndRewindsTheStream()
    {
        var content = SampleFiles.Pdf;
        using var stream = new MemoryStream(content);

        var inspection = await _inspector.InspectAsync(
            stream, clientExtension: "pdf", declaredContentType: "application/pdf", FilePurpose.Attachment,
            TestContext.Current.CancellationToken);

        inspection.Sha256.Should().Equal(SHA256.HashData(content));
        inspection.SizeBytes.Should().Be(content.Length);
        stream.Position.Should().Be(expected: 0);
    }

    [Fact]
    public async Task InspectAsync_WithAcceptedFile_StartsOneSpanWithTheTags()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Files.Source);
        using var parent = new Activity(operationName: "test").Start();
        using var stream = new MemoryStream(SampleFiles.Png);

        await _inspector.InspectAsync(
            stream, clientExtension: "png", declaredContentType: "image/png", FilePurpose.Attachment,
            TestContext.Current.CancellationToken);

        var spans = capture.Spans.Where(span => span.TraceId == parent.TraceId).ToList();

        spans.Should().ContainSingle(span => span.OperationName == ObservabilityConstants.Files.Spans.Inspect);
        spans.Single().Kind.Should().Be(ActivityKind.Internal);
        spans.Single().GetTagItem(ObservabilityConstants.Files.Tags.DetectedType).Should().Be(expected: "png");
        spans.Single().GetTagItem(ObservabilityConstants.Files.Tags.Purpose).Should().Be(expected: "attachment");
        spans.Single().GetTagItem(ObservabilityConstants.Files.Tags.Result).Should().Be(expected: "accepted");
    }

    [Fact]
    public async Task InspectAsync_WithRefusedFile_SetsResultRejected()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Files.Source);
        using var parent = new Activity(operationName: "test").Start();
        using var stream = new MemoryStream(SampleFiles.Docm);

        await _inspector.InspectAsync(
            stream,
            clientExtension: "docx",
            declaredContentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FilePurpose.Attachment,
            TestContext.Current.CancellationToken);

        var span = capture.Spans.Single(candidate => candidate.TraceId == parent.TraceId);

        span.GetTagItem(ObservabilityConstants.Files.Tags.Result).Should().Be(expected: "rejected");
    }
}
