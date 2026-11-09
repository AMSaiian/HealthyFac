using System.Net.Http.Headers;
using HealthyFac.Document.Domain.Files;

namespace HealthyFac.Document.Application.Files;

/// <summary>
/// Builds the <c>Content-Disposition</c> value of a stored file. The upload stores it with the blob, and the link
/// repeats it in the signed response headers, so both use this one function.
/// </summary>
public static class FileContentDisposition
{
    private const string Inline = "inline";
    private const string Attachment = "attachment";
    private const char Replacement = '_';

    /// <summary>
    /// Builds <c>inline</c> for an image, and <c>attachment</c> with the file name for every other type. The file name
    /// goes in the ASCII form and in the RFC 5987 form, so a browser saves a non-ASCII name correctly.
    /// </summary>
    /// <param name="type">The stored type of the file.</param>
    /// <param name="fileName">The stored name without the extension.</param>
    /// <returns>The header value.</returns>
    public static string Build(AllowedFileType type, string fileName)
    {
        if (type.IsImage)
        {
            return Inline;
        }

        var fullName = $"{fileName}.{type.Extension}";
        var header = new ContentDispositionHeaderValue(Attachment)
        {
            FileName = ToAsciiName(fullName),
            FileNameStar = fullName,
        };

        return header.ToString();
    }

    private static string ToAsciiName(string name) =>
        string.Create(
            name.Length,
            name,
            static (span, source) =>
            {
                for (var index = 0; index < source.Length; index++)
                {
                    var character = source[index];
                    var isSafe = character is > ' ' and < '\u007f' and not '"' and not '\\' and not '%'
                        || character == ' ';
                    span[index] = isSafe ? character : Replacement;
                }
            });
}
