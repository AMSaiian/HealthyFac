using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using FileSignatures;
using HealthyFac.Document.Application.Files.Inspection;
using HealthyFac.Document.Domain.Files;
using HealthyFac.Document.Infrastructure.Observability;
using HealthyFac.Shared.Application.Observability;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Files.Tags;
using Values = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Files.Values;

namespace HealthyFac.Document.Infrastructure.Files.Inspection;

/// <summary>
/// Proves the type of a file with FileSignatures. A binary format comes from the signature of the content. A text type
/// (<c>txt</c>, <c>csv</c>, <c>md</c>) has no signature, so strict UTF-8 text decides when no binary format matches.
/// </summary>
/// <remarks>
/// The inspector is a singleton: the format inspector is built once. A damaged file can make the format readers throw
/// (for example an invalid compound file header). The inspector treats every such failure as "not an accepted file",
/// because the bytes come from an untrusted client. The span carries the stored extension of the
/// detected type, the purpose, and the result. It never carries a file name, an ID, or a size.
/// </remarks>
public sealed class FileSignaturesFileInspector : IFileInspector
{
    private const int ChunkBytes = 81920;
    private const string OctetStream = "application/octet-stream";

    private static readonly string[] TextExtensions = ["txt", "csv", "md"];

    private readonly FileFormatInspector _formatInspector = new();

    public async Task<FileInspection> InspectAsync(
        Stream seekableContent,
        string? clientExtension,
        string declaredContentType,
        FilePurpose purpose,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        using var activity = DocumentActivitySource.Instance.StartActivity(
            ObservabilityConstants.Files.Spans.Inspect, ActivityKind.Internal);

        seekableContent.Position = 0;
        var (sizeBytes, sha256) = await HashAsync(seekableContent, cancellationToken);

        var detectedType = await DetectTypeAsync(seekableContent, clientExtension, cancellationToken);
        seekableContent.Position = 0;

        var acceptedType = detectedType is not null
            && detectedType == AllowedFileTypes.FindByExtension(clientExtension)
            && IsDeclaredTypeAgreeing(detectedType, declaredContentType)
            && detectedType.IsAllowedFor(purpose)
                ? detectedType
                : null;

        activity?.SetTag(Tags.DetectedType, detectedType?.Extension ?? Values.UnknownType);
        activity?.SetTag(Tags.Purpose, purpose.Value);
        activity?.SetTag(Tags.Result, acceptedType is null ? Values.Rejected : Values.Accepted);

        return new FileInspection { Type = acceptedType, SizeBytes = sizeBytes, Sha256 = sha256 };
    }

    private static async Task<(long SizeBytes, byte[] Sha256)> HashAsync(
        Stream content,
        CancellationToken cancellationToken
    )
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[ChunkBytes];
        long total = 0;
        var read = await content.ReadAsync(buffer, cancellationToken);

        while (read > 0)
        {
            hash.AppendData(buffer, offset: 0, read);
            total += read;
            read = await content.ReadAsync(buffer, cancellationToken);
        }

        return (total, hash.GetHashAndReset());
    }

    private static bool IsDeclaredTypeAgreeing(AllowedFileType type, string declaredContentType)
    {
        var isTextType = TextExtensions.Contains(type.Extension);
        var isTypeless = string.IsNullOrWhiteSpace(declaredContentType)
            || declaredContentType.StartsWith(OctetStream, StringComparison.OrdinalIgnoreCase);

        if (isTextType && isTypeless)
        {
            return true;
        }

        return MediaTypeHeaderValue.TryParse(declaredContentType, out var declared)
            && string.Equals(declared.MediaType, type.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AllowedFileType?> DetectTypeAsync(
        Stream content,
        string? clientExtension,
        CancellationToken cancellationToken
    )
    {
        content.Position = 0;
        FileFormat? format;

        try
        {
            format = _formatInspector.DetermineFileFormat(content);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }

        if (format is not null)
        {
            return FileFormatMap.Find(format);
        }

        if (clientExtension is null || !TextExtensions.Contains(clientExtension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        content.Position = 0;
        var isText = await Utf8TextCheck.IsTextAsync(content, cancellationToken);

        return isText ? AllowedFileTypes.FindByExtension(clientExtension) : null;
    }
}
