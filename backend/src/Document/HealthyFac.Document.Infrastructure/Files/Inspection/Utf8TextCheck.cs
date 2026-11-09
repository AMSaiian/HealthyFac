using System.Text;

namespace HealthyFac.Document.Infrastructure.Files.Inspection;

/// <summary>
/// Decides whether a stream is plain text: strict UTF-8 with an optional byte order mark and no NUL byte.
/// </summary>
public static class Utf8TextCheck
{
    private const int ChunkBytes = 81920;
    private const byte NulByte = 0;

    /// <summary>
    /// Reads the whole stream in chunks and decodes it with a strict decoder.
    /// </summary>
    /// <param name="content">The stream to check, positioned at its start.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns><see langword="true"/> when the content is valid UTF-8 text without a NUL byte.</returns>
    public static async Task<bool> IsTextAsync(Stream content, CancellationToken cancellationToken)
    {
        var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        var buffer = new byte[ChunkBytes];
        var read = await content.ReadAsync(buffer, cancellationToken);

        try
        {
            while (read > 0)
            {
                if (buffer.AsSpan(start: 0, read).Contains(NulByte))
                {
                    return false;
                }

                _ = decoder.GetCharCount(buffer, index: 0, read, flush: false);
                read = await content.ReadAsync(buffer, cancellationToken);
            }

            _ = decoder.GetCharCount(buffer, index: 0, count: 0, flush: true);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        return true;
    }
}
